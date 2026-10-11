using Deployguard.Data;
using Deployguard.Services;
using Deployguard.Services.Pipeline;
using Deployguard.Services.Providers;
using Deployguard.Services.Reports;
using Deployguard.Services.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

// ============================================================================
// Load .env File if present (Local Development & Docker support)
// ============================================================================
LoadDotEnv();

var builder = WebApplication.CreateBuilder(args);

// Configure port for Render and container environments (Production / Cloud)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port) && !builder.Environment.IsDevelopment())
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

QuestPDF.Settings.License = LicenseType.Community;

// MVC
builder.Services.AddControllersWithViews();

// HTTP Client
builder.Services.AddHttpClient();

// Authentication
builder.Services.AddAuthentication(
    CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;
    });

// Database - Support both standard ADO.NET and Render's DATABASE_URL
var defaultConn = builder.Configuration.GetConnectionString("DefaultConnection");
var rawConnectionString = !string.IsNullOrWhiteSpace(defaultConn)
    ? defaultConn
    : builder.Configuration["DATABASE_URL"]
      ?? builder.Configuration["DATABASE_URL_UNPOOLED"]
      ?? Environment.GetEnvironmentVariable("DATABASE_URL")
      ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

var connectionString = ParsePostgreSqlConnectionString(rawConnectionString);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        options.UseNpgsql(connectionString);
    }
    else
    {
        // Fallback default for development if nothing specified
        options.UseNpgsql("Host=localhost;Port=5432;Database=deployguard;Username=postgres;Password=postgres;");
    }
});

// Repository services
builder.Services.AddScoped<RepositoryUrlParser>();
builder.Services.AddScoped<RepositoryService>();

// Security services
builder.Services.AddScoped<RepositoryCloneService>();
builder.Services.AddScoped<SecretScannerService>();
builder.Services.AddScoped<SecretScanService>();

// Pipeline services
builder.Services.AddScoped<PipelineParserService>();
builder.Services.AddScoped<PipelineDetectionService>();

// Report services
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<PdfReportService>();
builder.Services.AddScoped<AdminPdfReportService>();

// Git providers
builder.Services.AddScoped<IGitProvider, GitHubProvider>();
builder.Services.AddScoped<IGitProvider, GitLabProvider>();
builder.Services.AddScoped<IGitProvider, BitbucketProvider>();
builder.Services.AddScoped<IGitProvider, AzureDevOpsProvider>();

// Build application
var app = builder.Build();

// Database Migration & Seed initial data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        // Automatically apply EF Core migrations if database is connected
        if (context.Database.IsRelational())
        {
            await context.Database.MigrateAsync();
        }

        await DbSeeder.SeedAsync(
            context,
            builder.Configuration);

        await SecretRuleSeeder.SeedAsync(
            context);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during database migration/seeding.");
    }
}

// Error handling
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Middleware
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Health check endpoint
app.MapGet("/health", () =>
{
    var uptime = (DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalSeconds;
    return Results.Ok(new
    {
        status = "ok",
        timestamp = DateTime.UtcNow.ToString("o"),
        uptime = uptime
    });
}).AllowAnonymous();

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// ============================================================================
// Helper Methods: .env Loader & Connection String Parser
// ============================================================================

static void LoadDotEnv()
{
    var searchPaths = new[]
    {
        Path.Combine(Directory.GetCurrentDirectory(), ".env"),
        Path.Combine(AppContext.BaseDirectory, ".env"),
        Path.Combine(Directory.GetCurrentDirectory(), "..", ".env")
    };

    foreach (var filePath in searchPaths)
    {
        if (File.Exists(filePath))
        {
            foreach (var line in File.ReadAllLines(filePath))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
                {
                    continue;
                }

                var separatorIndex = trimmed.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var key = trimmed[..separatorIndex].Trim();
                var value = trimmed[(separatorIndex + 1)..].Trim();

                // Strip surrounding quotes if present
                if ((value.StartsWith('"') && value.EndsWith('"')) ||
                    (value.StartsWith('\'') && value.EndsWith('\'')))
                {
                    value = value[1..^1];
                }

                // Only set if not already set by system/cloud provider
                if (Environment.GetEnvironmentVariable(key) == null)
                {
                    Environment.SetEnvironmentVariable(key, value);
                }
            }
            break; // Stop after loading the first found .env
        }
    }
}

static string? ParsePostgreSqlConnectionString(string? input)
{
    if (string.IsNullOrWhiteSpace(input))
    {
        return input;
    }

    // Convert Heroku/Render URI format (postgres:// or postgresql://) to Npgsql format
    if (input.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
        input.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        try
        {
            var uri = new Uri(input);
            var userInfo = uri.UserInfo.Split(':');
            var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "";
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : 5432;
            var database = uri.AbsolutePath.TrimStart('/');

            return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;";
        }
        catch
        {
            return input;
        }
    }

    return input;
}