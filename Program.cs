using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using TripAnalyzer.Data;
using TripAnalyzer.Models;
using TripAnalyzer.Services;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// Render / Reverse Proxy HTTPS Configuration
// ============================================================
// Render terminates HTTPS at its proxy and forwards the request
// to the Docker container. This makes ASP.NET Core see HTTP unless
// the forwarded headers are processed.
//
// This is required so Google OAuth generates:
// https://trip-analyzer.onrender.com/signin-google
// instead of:
// http://trip-analyzer.onrender.com/signin-google
// ============================================================

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;

    // Render is a reverse proxy. Clear the default restrictions
    // so forwarded headers are accepted from the proxy.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// ============================================================
// 1. Database Connection Configuration
// ============================================================

string? rawDbUrl =
    builder.Configuration["DATABASE_URL"]
    ?? Environment.GetEnvironmentVariable("DATABASE_URL");

string connectionString =
    DatabaseUrlParser.ConvertConnectionString(rawDbUrl);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// ============================================================
// 2. Identity Configuration
// ============================================================

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password policy
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;

    options.User.RequireUniqueEmail = true;

    // Account lockout
    options.Lockout.DefaultLockoutTimeSpan =
        TimeSpan.FromMinutes(15);

    options.Lockout.MaxFailedAccessAttempts = 5;

    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ============================================================
// Authentication Cookie Configuration
// ============================================================

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";

    options.Cookie.Name = "TripAnalyzer.AuthSession";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;

    // HTTPS cookie in production
    options.Cookie.SecurePolicy =
        CookieSecurePolicy.Always;

    options.Cookie.MaxAge = null;

    options.ExpireTimeSpan =
        TimeSpan.FromHours(2);

    options.SlidingExpiration = true;
});

// ============================================================
// Google Authentication
// ============================================================

var googleClientId =
    builder.Configuration["Authentication:Google:ClientId"]
    ?? builder.Configuration["Authentication__Google__ClientId"]
    ?? Environment.GetEnvironmentVariable(
        "Authentication__Google__ClientId");

var googleClientSecret =
    builder.Configuration["Authentication:Google:ClientSecret"]
    ?? builder.Configuration["Authentication__Google__ClientSecret"]
    ?? Environment.GetEnvironmentVariable(
        "Authentication__Google__ClientSecret");

if (!string.IsNullOrEmpty(googleClientId) &&
    !string.IsNullOrEmpty(googleClientSecret))
{
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;

            // Google OAuth callback
            options.CallbackPath = "/signin-google";
        });
}

// ============================================================
// 3. Rate Limiting
// ============================================================

builder.Services.AddRateLimiter(options =>
{
    // --------------------------------------------------------
    // Login: 5 attempts per 15 minutes per IP
    // --------------------------------------------------------

    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey:
                httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",

            factory: _ =>
                new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(15),
                    QueueProcessingOrder =
                        QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }));

    // --------------------------------------------------------
    // Contact: 3 submissions per hour per IP
    // --------------------------------------------------------

    options.AddPolicy("contact", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey:
                httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",

            factory: _ =>
                new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 3,
                    Window = TimeSpan.FromHours(1),
                    QueueProcessingOrder =
                        QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }));

    // --------------------------------------------------------
    // Chatbot: 20 requests per minute per IP
    // --------------------------------------------------------

    options.AddPolicy("chatbot", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey:
                httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",

            factory: _ =>
                new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder =
                        QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }));

    // --------------------------------------------------------
    // Trip Analysis: 20 requests per minute per IP
    // --------------------------------------------------------

    options.AddPolicy("trip", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey:
                httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",

            factory: _ =>
                new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder =
                        QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }));

    // --------------------------------------------------------
    // District Search: 60 requests per minute per IP
    // --------------------------------------------------------

    options.AddPolicy("districts", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey:
                httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",

            factory: _ =>
                new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 60,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder =
                        QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }));

    // Return HTTP 429 when limit is exceeded
    options.RejectionStatusCode = 429;
});

// ============================================================
// 4. Service Registrations
// ============================================================

builder.Services.AddScoped<
    ITripRecommendationService,
    TripRecommendationService>();

builder.Services.AddScoped<
    IChatbotService,
    ChatbotService>();

builder.Services.AddTransient<
    IEmailSenderService,
    EmailSenderService>();

builder.Services.AddScoped<
    IAdminAnalyticsService,
    AdminAnalyticsService>();

builder.Services.AddScoped<
    IDistrictService,
    DistrictService>();

builder.Services.AddScoped<
    IDistanceCalculationService,
    DistanceCalculationService>();

builder.Services.AddScoped<
    IFareCalculationService,
    FareCalculationService>();

builder.Services.AddScoped<
    IRoutingService,
    RoutingService>();

builder.Services.AddControllersWithViews();

// ============================================================
// Build Application
// ============================================================

var app = builder.Build();

// ============================================================
// 5. Automatic Database Migration & Seeding
// ============================================================

try
{
    await DbInitializer.SeedAsync(
        app.Services,
        app.Configuration);
}
catch (Exception ex)
{
    var logger =
        app.Services.GetRequiredService<ILogger<Program>>();

    logger.LogError(
        ex,
        "An error occurred while seeding the database.");
}

// ============================================================
// 6. Middleware Pipeline
// ============================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// ============================================================
// IMPORTANT:
// Process Render's forwarded HTTPS header BEFORE
// UseHttpsRedirection and Authentication.
// ============================================================

app.UseForwardedHeaders();

app.UseHttpsRedirection();

app.UseStaticFiles();

// ============================================================
// Security Headers
// ============================================================

app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.Append(
        "X-Content-Type-Options",
        "nosniff");

    ctx.Response.Headers.Append(
        "X-Frame-Options",
        "SAMEORIGIN");

    ctx.Response.Headers.Append(
        "Referrer-Policy",
        "strict-origin-when-cross-origin");

    ctx.Response.Headers.Append(
        "Permissions-Policy",
        "geolocation=(self), camera=(), microphone=()");

    await next();
});

// ============================================================
// Routing
// ============================================================

app.UseRouting();

// ============================================================
// Rate Limiting
// ============================================================

app.UseRateLimiter();

// ============================================================
// Authentication & Authorization
// ============================================================

app.UseAuthentication();

app.UseAuthorization();

// ============================================================
// Area Routes
// ============================================================

app.MapControllerRoute(
    name: "areas",
    pattern:
        "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// ============================================================
// Default Routes
// ============================================================

app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Home}/{action=Index}/{id?}");

// ============================================================
// Run
// ============================================================

app.Run();