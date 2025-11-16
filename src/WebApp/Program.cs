using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
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

    var root = Directory.GetCurrentDirectory();
    var envFile = Path.Combine(root, ".dev.env");
    DotEnv.Load(envFile);

    var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ??
                           throw new InvalidOperationException("CONNECTION_STRING environment variable not found.");

    var googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ??
                         throw new InvalidOperationException("GOOGLE_CLIENT_ID environment variable not found.");

    var googleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET") ??
                             throw new InvalidOperationException("GOOGLE_CLIENT_SECRET environment variable not found.");

    var healthCheckApiKey = Environment.GetEnvironmentVariable("HEALTHCHECKS_API_KEY") ??
                            throw new InvalidOperationException("HEALTHCHECKS_API_KEY environment variable not found.");

    var mvcBuilder = builder.Services.AddControllersWithViews();
    builder.Services.AddRazorPages();

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
        });

    builder.Services.AddTransient<GlobalExceptionHandlerMiddleware>();

    builder.Services.AddSingleton<IAuthorizationHandler, AdminOrApiKeyHandler>();

    builder.Services.AddApplicationServices();

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddSingleton<DbMigrationService>();
    builder.Services.AddHostedService(sp => sp.GetRequiredService<DbMigrationService>());

    builder.Services.AddHealthChecks()
        .AddNpgSql(connectionString);

    builder.Services.AddHealthChecksUI(setup =>
    {
        setup.AddHealthCheckEndpoint("API", "https://web/health");

        setup.UseApiEndpointHttpMessageHandler(_ => new HealthCheckHttpClientHandler(
            healthCheckApiKey,
            builder.Environment.IsDevelopment()
        ));
    })
    .AddInMemoryStorage();

    builder.Services.AddAutoMapper(cfg =>
    {
        cfg.AddMaps(typeof(Program));
    });

    builder.Services.AddResponseCaching();

    var app = builder.Build();

    app.UseGlobalExceptionHandler();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();

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

    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            if (httpContext.Request.Host.Value != null)
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent);
        };
        options.GetLevel = (httpContext, elapsed, ex) =>
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

    app.MapHealthChecksUI(options =>
    {
        options.UIPath = "/health-ui";
        options.ApiPath = "/health-api";
    }).RequireAuthorization("Admin");

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

public partial class Program { }