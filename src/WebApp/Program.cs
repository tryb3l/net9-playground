using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using System.Threading.RateLimiting;
using WebApp.Data;
using WebApp.Extensions;
using WebApp.Middleware;
using WebApp.Models;
using WebApp.Services;
using WebApp.Utils;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
        .MinimumLevel.Override("HealthChecks.UI", LogEventLevel.Warning)
        .WriteTo.Console());

    if (builder.Environment.IsDevelopment())
    {
        var root = Directory.GetCurrentDirectory();
        var envFile = Path.Combine(root, ".dev.env");
        DotEnv.Load(envFile);
    }

    var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ??
                           throw new InvalidOperationException("CONNECTION_STRING environment variable not found.");

    var googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ??
                         throw new InvalidOperationException("GOOGLE_CLIENT_ID environment variable not found.");

    var googleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET") ??
                             throw new InvalidOperationException("GOOGLE_CLIENT_SECRET environment variable not found.");

    // Not consumed here: AdminOrApiKeyHandler reads it per-request. Validated at startup so a
    // missing key fails fast instead of silently locking /health to interactive admins only.
    _ = Environment.GetEnvironmentVariable("HEALTHCHECKS_API_KEY") ??
        throw new InvalidOperationException("HEALTHCHECKS_API_KEY environment variable not found.");

    var mvcBuilder = builder.Services.AddControllersWithViews(options =>
    {
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    });

    // Single-author blog: self-registration is closed unless explicitly switched on.
    var registrationEnabled = builder.Configuration.GetValue("Blog:RegistrationEnabled", false);

    builder.Services.AddRazorPages(options =>
    {
        options.Conventions.AuthorizeAreaPage("Identity", "/Account/Register", "RegistrationOpen");
    });

    if (builder.Environment.IsDevelopment())
    {
        mvcBuilder.AddRazorRuntimeCompilation();
    }

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));

    builder.Services.AddIdentity<User, IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders()
        .AddDefaultUI();

    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Identity/Account/Login";
        options.AccessDeniedPath = "/Identity/Account/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/health"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/health"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

    builder.Services.AddAntiforgery(options =>
    {
        options.HeaderName = "RequestVerificationToken";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
    });

    builder.Services.AddAuthentication()
        .AddGoogle(googleOptions =>
        {
            googleOptions.ClientId = googleClientId;
            googleOptions.ClientSecret = googleClientSecret;
            googleOptions.CallbackPath = "/signin-google";
        });

    builder.Services.AddAuthorizationBuilder()
        .AddPolicy("Admin", policy =>
        {
            policy.AddRequirements(new AdminOrApiKeyRequirement());
        })
        .AddPolicy("RegistrationOpen", policy =>
        {
            policy.RequireAssertion(_ => registrationEnabled);
        });

    builder.Services.AddTransient<GlobalExceptionHandlerMiddleware>();

    builder.Services.AddSingleton<IAuthorizationHandler, AdminOrApiKeyHandler>();

    builder.Services.AddApplicationServices();

    builder.Services.AddHttpContextAccessor();

    if (Environment.GetEnvironmentVariable("DISABLE_DB_SEEDING") != "true")
    {
        builder.Services.AddHostedService<DbMigrationService>();
    }
    builder.Services.AddHealthChecks()
        .AddNpgSql(connectionString);

    builder.Services.AddResponseCaching();

    builder.Services.AddRateLimiter(options =>
    {
        options.AddPolicy("upload", context =>
        {
            var userId = context.User?.Identity?.Name
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous";
            return RateLimitPartition.GetFixedWindowLimiter(userId, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            });
        });

        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.HttpContext.Response.WriteAsync(
                "Upload rate limit exceeded. Try again in a minute.", cancellationToken);
        };
    });

    var app = builder.Build();

    // Outermost handler: renders a real HTML page for browser requests. The JSON handler
    // below only claims API/AJAX requests and lets everything else fall through to here.
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseGlobalExceptionHandler();

    // 404s and other bare status codes get the same page, except on the JSON endpoints
    // where an HTML body would be worse than none.
    app.UseWhen(
        context => !context.Request.Path.StartsWithSegments("/Admin/api")
                   && !context.Request.Path.StartsWithSegments("/health"),
        branch => branch.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}"));

    app.UseHttpsRedirection();

    app.Use(async (context, next) =>
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers.TryAdd("X-Content-Type-Options", "nosniff");
            headers.TryAdd("X-Frame-Options", "DENY");
            headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
            headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");

            // CDN hosts used by admin pages (DataTables, Bootstrap Icons,
            // Toastr, EasyMDE, Choices.js). Public pages only use cdn.jsdelivr.net.
            const string cdnSources = "cdn.jsdelivr.net cdnjs.cloudflare.com cdn.datatables.net unpkg.com";
            var csp = string.Join("; ",
                "default-src 'self'",
                $"script-src 'self' {cdnSources} 'unsafe-inline'",
                $"style-src 'self' {cdnSources} 'unsafe-inline'",
                $"font-src 'self' {cdnSources}",
                "img-src 'self' data:",
                "connect-src 'self'",
                "object-src 'none'",
                "base-uri 'self'",
                "frame-ancestors 'none'"
            );
            headers.TryAdd("Content-Security-Policy", csp);

            return Task.CompletedTask;
        });

        await next();
    });

    app.UseResponseCaching();

    if (app.Environment.IsDevelopment())
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
                ctx.Context.Response.Headers.Append("Pragma", "no-cache");
                ctx.Context.Response.Headers.Append("Expires", "0");
            }
        });
    }
    else
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                if (ctx.Context.Request.Path.StartsWithSegments("/uploads"))
                {
                    var headers = ctx.Context.Response.Headers;
                    // Cache for 1 year
                    headers.CacheControl = "public,max-age=31536000,immutable";
                }
            }
        });
    }

    app.UseRouting();

    app.UseRateLimiter();

    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            if (httpContext.Request.Host.Value != null)
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent);
        };
        options.GetLevel = (httpContext, _, ex) =>
        {
            if (ex == null && httpContext.Response.StatusCode == 200 && httpContext.Request.Path.StartsWithSegments("/health"))
            {
                return LogEventLevel.Debug;
            }
            return LogEventLevel.Information;
        };
    });

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapStaticAssets();

    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        Predicate = _ => true,
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    }).DisableHttpMetrics().RequireAuthorization("Admin");

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false
    }).AllowAnonymous();

    app.MapControllerRoute(
        name: "admin",
        pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.MapRazorPages()
       .WithStaticAssets();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    Log.Information("Shut down complete");
    Log.CloseAndFlush();
}