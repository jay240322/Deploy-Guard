using Deployguard.Data;
using Deployguard.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace Deployguard.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Forbid();
        }

        var user = await _context.AppUsers
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return NotFound();
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

            RecentRepositories = repositories
                .Take(5)
                .ToList(),

            RecentScans = scans
                .Take(5)
                .ToList()
        };

        return View(dashboard);
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
                RequestId =
                    Activity.Current?.Id
                    ?? HttpContext.TraceIdentifier
            });
    }
}