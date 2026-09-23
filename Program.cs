using Deployguard.Data;
using Deployguard.Services;
using Deployguard.Services.Pipeline;
using Deployguard.Services.Providers;
using Deployguard.Services.Reports;
using Deployguard.Services.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

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

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
options.UseNpgsql(
builder.Configuration.GetConnectionString("DefaultConnection")
));

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

// Git providers
builder.Services.AddScoped<IGitProvider, GitHubProvider>();
builder.Services.AddScoped<IGitProvider, GitLabProvider>();
builder.Services.AddScoped<IGitProvider, BitbucketProvider>();
builder.Services.AddScoped<IGitProvider, AzureDevOpsProvider>();

// Build application
var app = builder.Build();

// Seed initial data
using (var scope = app.Services.CreateScope())
{
var context = scope.ServiceProvider
.GetRequiredService<ApplicationDbContext>();

await DbSeeder.SeedAsync(
    context,
    builder.Configuration);

await SecretRuleSeeder.SeedAsync(
    context);

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

// Default route
app.MapControllerRoute(
name: "default",
pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();