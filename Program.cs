using Deployguard.Data;
using Deployguard.Services;
using Deployguard.Services.Providers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

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

// providers
builder.Services.AddScoped<IGitProvider, GitHubProvider>();
builder.Services.AddScoped<IGitProvider, GitLabProvider>();
builder.Services.AddScoped<IGitProvider, BitbucketProvider>();
builder.Services.AddScoped<IGitProvider, AzureDevOpsProvider>();
// Build application
var app = builder.Build();

// Seed initial Admin account
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    await DbSeeder.SeedAsync(
        context,
        builder.Configuration);
}

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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();