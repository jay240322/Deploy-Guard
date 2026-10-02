using Deployguard.Data;
using Deployguard.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace Deployguard.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        // If user is not authenticated, show the public Landing Page
        if (User.Identity?.IsAuthenticated != true)
        {
            return View("Landing");
        }

        // If user is Admin, redirect to Admin dashboard
        if (User.IsInRole("Admin"))
        {
            return RedirectToAction("Index", "Admin");
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return View("Landing");
        }

        var user = await _context.AppUsers
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return View("Landing");
        }

        var repositories = await _context.Repositories
            .Where(r => r.CreatedByUserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var scans = await _context.SecretScans
            .Include(s => s.Repository)
            .Where(s => s.TriggeredByUserId == userId)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync();

        var dashboard = new UserDashboardViewModel
        {
            UserName = user.UserName,
            FullName = user.FullName,
            RepositoryCount = repositories.Count,
            ScanCount = scans.Count,
            SecretsFound = scans.Sum(s => s.SecretsFound),
            RecentRepositories = repositories.Take(5).ToList(),
            RecentScans = scans.Take(5).ToList()
        };

        return View("Index", dashboard);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Dashboard()
    {
        return await Index();
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true)]
    public IActionResult Error()
    {
        return View(
            new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
    }
}