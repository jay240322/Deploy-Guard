using Deployguard.Data;
using Deployguard.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Deployguard.Controllers;

public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AdminController(
        ApplicationDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    // Admin login page
    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true &&
            User.IsInRole("Admin"))
        {
            return RedirectToAction(nameof(Index));
        }

        return View();
    }

    // Admin login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "Admin password is required.";
            return View();
        }

        var adminPassword = "Admin@12345";

        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            ViewBag.Error =
                "Admin password is not configured.";

            return View();
        }

        if (password != adminPassword)
        {
            ViewBag.Error = "Invalid admin password.";
            return View();
        }

        var admin = await _context.AppUsers
            .FirstOrDefaultAsync(u =>
                u.Role == "Admin" &&
                u.IsActive);

        if (admin == null)
        {
            ViewBag.Error = "Admin account was not found.";
            return View();
        }

        var claims = new List<Claim>
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                admin.Id.ToString()),

            new Claim(
                ClaimTypes.Name,
                admin.UserName),

            new Claim(
                ClaimTypes.Email,
                admin.Email),

            new Claim(
                ClaimTypes.Role,
                "Admin")
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal);

        return RedirectToAction(nameof(Index));
    }

    // Admin Dashboard
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // Not logged in
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction(nameof(Login));
        }

        // Logged in but not an Admin
        if (!User.IsInRole("Admin"))
        {
            return Forbid();
        }

        var recentUsers = await _context.AppUsers
            .OrderByDescending(u => u.CreatedAt)
            .Take(5)
            .ToListAsync();

        var recentRepositories = await _context.Repositories
            .Include(r => r.CreatedByUser)
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .ToListAsync();

        var recentScans = await _context.SecretScans
            .Include(s => s.Repository)
            .Include(s => s.TriggeredByUser)
            .OrderByDescending(s => s.StartedAt)
            .Take(5)
            .ToListAsync();

        var dashboard = new AdminDashboardViewModel
        {
            TotalUsers =
                await _context.AppUsers.CountAsync(),

            TotalRepositories =
                await _context.Repositories.CountAsync(),

            TotalScans =
                await _context.SecretScans.CountAsync(),

            TotalSecretsFound =
                await _context.SecretScans
                    .SumAsync(s => s.SecretsFound),

            RecentUsers = recentUsers,
            RecentRepositories = recentRepositories,
            RecentScans = recentScans
        };

        return View(dashboard);
    }
}