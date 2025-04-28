using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.HealthChecks;
using WebApplication1.Interfaces;
using WebApplication1.Middleware;
using WebApplication1.Models;
using WebApplication1.Repositories;
using WebApplication1.Services;
using WebApplication1.Utils;

var builder = WebApplication.CreateBuilder(args);

var root = Directory.GetCurrentDirectory();
var envFile = Path.Combine(root, ".dev.env");
DotEnv.Load(envFile);

var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ??
throw new InvalidOperationException("CONNECTION_STRING environment variable not found.");

var googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ??
throw new InvalidOperationException("GOOGLE_CLIENT_ID environment variable not found.");

var googleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET") ??
throw new InvalidOperationException("GOOGLE_CLIENT_SECRET environment variable not found.");

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

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

// Add global error handling
builder.Services.AddTransient<GlobalExceptionHandlerMiddleware>();

// Repository registrations
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<ITagRepository, TagRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();

// Service registrations
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IAccountService, AccountService>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<DbMigrationService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<DbMigrationService>());
builder.Services.AddHealthChecks()
    .AddCheck<DbMigrationHealthChecks>("database_migrations", tags: ["database"]);

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHttpClient("HealthChecksClient", client =>
    {
        client.BaseAddress = new Uri("http://web/");
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback =
            (sender, certificate, chain, sslPolicyErrors) => true
    });

    builder.Services.AddHealthChecksUI(options =>
    {
        options.SetEvaluationTimeInSeconds(5);
        options.MaximumHistoryEntriesPerEndpoint(50);
        options.AddHealthCheckEndpoint("API", "http://web/health");
        options.UseApiEndpointHttpMessageHandler(sp =>
            new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    (sender, certificate, chain, sslPolicyErrors) => true
            });
    }).AddInMemoryStorage();
}
else
{
    builder.Services.AddHealthChecksUI(options =>
    {
        options.SetEvaluationTimeInSeconds(5);
        options.MaximumHistoryEntriesPerEndpoint(50);
        options.AddHealthCheckEndpoint("API", "http://web/health");
    }).AddInMemoryStorage();
}

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

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

    string csp = "default-src 'self'; " +
                 "script-src 'self' 'sha256-V5ld3fn8GVclauMRqI82QiZ10Q9Y3gzkMrZheQtM4mA='; " +
                 "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net/npm/; " +
                 "img-src 'self' data: https://avatars3.githubusercontent.com https://ui-avatars.com https://lh3.googleusercontent.com; " +
                 "font-src 'self' data: https://cdn.jsdelivr.net/npm/; " +
                 "connect-src 'self'; " +
                 "frame-src 'self'; " +
                 "object-src 'none'; " +
                 "base-uri 'self'; " +
                 "form-action 'self'; " +
                 "frame-ancestors 'self';";
    context.Response.Headers.Append("Content-Security-Policy", csp);

    context.Response.Headers.Append("Permissions-Policy", "camera=(), geolocation=(), gyroscope=(), microphone=(), payment=(), usb=()");

    await next();
});

app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).DisableHttpMetrics();

app.MapHealthChecksUI(options =>
{
    options.UIPath = "/health-ui";
    options.ApiPath = "/health-api";
});

app.MapControllerRoute(
    name: "admin",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
    );

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages()
   .WithStaticAssets();

app.Run();