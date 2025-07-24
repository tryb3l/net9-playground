using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Serilog;
using WebApplication1.Data;
using WebApplication1.Extensions;
using WebApplication1.Middleware;
using WebApplication1.Models;
using WebApplication1.Services;
using WebApplication1.Utils;

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
    builder.Services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
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

    var app = builder.Build();

    app.UseGlobalExceptionHandler();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();

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