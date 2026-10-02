using Deployguard.Data;
using Deployguard.Models;
using Deployguard.Services.Reports;
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
    private readonly AdminPdfReportService _pdfReportService;

    public AdminController(
        ApplicationDbContext context,
        IConfiguration configuration,
        AdminPdfReportService pdfReportService)
    {
        _context = context;
        _configuration = configuration;
        _pdfReportService = pdfReportService;
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

        var adminPassword = _configuration["DeployGuard:AdminPassword"]
            ?? _configuration["ADMIN_PASSWORD"]
            ?? "Admin@12345";

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
        if (!IsAdminAuthenticated(out _))
        {
            return RedirectToAction(nameof(Login));
        }

        var recentUsers = await _context.AppUsers
            .Where(u => u.Role != "Admin")
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
            TotalUsers = await _context.AppUsers.CountAsync(u => u.Role != "Admin"),
            TotalRepositories = await _context.Repositories.CountAsync(),
            TotalScans = await _context.SecretScans.CountAsync(),
            TotalSecretsFound = await _context.SecretScans.SumAsync(s => s.SecretsFound),
            RecentUsers = recentUsers,
            RecentRepositories = recentRepositories,
            RecentScans = recentScans
        };

        return View(dashboard);
    }

    [HttpGet]
    public IActionResult Dashboard()
    {
        return RedirectToAction(nameof(Index));
    }

    // =========================================================================
    // USERS MANAGEMENT SECTION
    // =========================================================================

    [HttpGet]
    public async Task<IActionResult> Users()
    {
        if (!IsAdminAuthenticated(out var currentUserId))
        {
            return RedirectToAction(nameof(Login));
        }

        var users = await _context.AppUsers
            .Where(u => u.Role != "Admin")
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new AdminUserItemViewModel
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                FullName = u.FullName,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                RepositoriesCount = _context.Repositories.Count(r => r.CreatedByUserId == u.Id),
                ScansCount = _context.SecretScans.Count(s => s.TriggeredByUserId == u.Id),
                SecretsFoundCount = _context.SecretScans
                    .Where(s => s.TriggeredByUserId == u.Id)
                    .Sum(s => (int?)s.SecretsFound) ?? 0
            })
            .ToListAsync();

        var model = new AdminUsersViewModel
        {
            Users = users,
            CurrentUserId = currentUserId
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(int id)
    {
        if (!IsAdminAuthenticated(out var currentUserId))
        {
            return RedirectToAction(nameof(Login));
        }

        if (id == currentUserId)
        {
            TempData["Error"] = "Action Denied: You cannot delete your own logged-in admin account.";
            return RedirectToAction(nameof(Users));
        }

        var user = await _context.AppUsers.FindAsync(id);
        if (user == null)
        {
            TempData["Error"] = "User was not found.";
            return RedirectToAction(nameof(Users));
        }

        // Clean up user's repositories and related data
        var repos = await _context.Repositories
            .Where(r => r.CreatedByUserId == id)
            .ToListAsync();

        foreach (var repo in repos)
        {
            var pipelineFiles = await _context.PipelineFiles
                .Where(p => p.RepositoryId == repo.Id)
                .ToListAsync();

            foreach (var pf in pipelineFiles)
            {
                var stages = await _context.PipelineStages
                    .Where(s => s.PipelineFileId == pf.Id)
                    .ToListAsync();
                _context.PipelineStages.RemoveRange(stages);
            }
            _context.PipelineFiles.RemoveRange(pipelineFiles);

            var scans = await _context.SecretScans
                .Where(s => s.RepositoryId == repo.Id)
                .ToListAsync();

            foreach (var scan in scans)
            {
                var findings = await _context.SecretFindings
                    .Where(f => f.SecretScanId == scan.Id)
                    .ToListAsync();
                _context.SecretFindings.RemoveRange(findings);
            }
            _context.SecretScans.RemoveRange(scans);
        }

        _context.Repositories.RemoveRange(repos);

        // Delete any remaining scans triggered by the user
        var userScans = await _context.SecretScans
            .Where(s => s.TriggeredByUserId == id)
            .ToListAsync();

        foreach (var scan in userScans)
        {
            var findings = await _context.SecretFindings
                .Where(f => f.SecretScanId == scan.Id)
                .ToListAsync();
            _context.SecretFindings.RemoveRange(findings);
        }
        _context.SecretScans.RemoveRange(userScans);

        // Delete user's audit logs
        var logs = await _context.AuditLogs
            .Where(a => a.UserId == id)
            .ToListAsync();
        _context.AuditLogs.RemoveRange(logs);

        // Finally delete the user
        _context.AppUsers.Remove(user);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"User '{user.UserName}' ({user.Email}) and all associated records have been permanently deleted.";
        return RedirectToAction(nameof(Users));
    }

    // =========================================================================
    // REPORT SECTION & LIVE ANALYTICS
    // =========================================================================

    [HttpGet]
    public async Task<IActionResult> Report()
    {
        if (!IsAdminAuthenticated(out _))
        {
            return RedirectToAction(nameof(Login));
        }

        var report = await GenerateLiveAdminReportAsync();
        return View(report);
    }

    [HttpGet]
    public async Task<IActionResult> ExportReportPdf()
    {
        if (!IsAdminAuthenticated(out _))
        {
            return RedirectToAction(nameof(Login));
        }

        var report = await GenerateLiveAdminReportAsync();
        var pdfBytes = _pdfReportService.Generate(report);

        var fileName = $"DeployGuard-Platform-Security-Report-{DateTime.UtcNow:yyyyMMdd-HHmm}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }

    // Helper: Build live report metrics on demand every time
    private async Task<AdminReportViewModel> GenerateLiveAdminReportAsync()
    {
        var totalUsers = await _context.AppUsers.CountAsync(u => u.Role != "Admin");
        var totalRepos = await _context.Repositories.CountAsync();
        var totalScans = await _context.SecretScans.CountAsync();
        var totalSecrets = await _context.SecretScans.SumAsync(s => s.SecretsFound);
        var totalFiles = await _context.SecretScans.SumAsync(s => s.FilesScanned);

        var completedScans = await _context.SecretScans
            .Where(s => s.Status == "Completed")
            .ToListAsync();

        double totalDurationSec = 0;
        foreach (var scan in completedScans)
        {
            if (scan.CompletedAt.HasValue && scan.CompletedAt > scan.StartedAt)
            {
                totalDurationSec += (scan.CompletedAt.Value - scan.StartedAt).TotalSeconds;
            }
        }

        double avgDurationSec = completedScans.Count > 0 ? (totalDurationSec / completedScans.Count) : 0;
        var failedScansCount = await _context.SecretScans.CountAsync(s => s.Status == "Failed");
        var runningScansCount = await _context.SecretScans.CountAsync(s => s.Status == "Running");

        // Calculate risk per repository
        var repositories = await _context.Repositories
            .Include(r => r.CreatedByUser)
            .Include(r => r.SecretScans)
                .ThenInclude(s => s.Findings)
            .ToListAsync();

        var riskyRepositories = new List<AdminRepositoryRiskViewModel>();

        foreach (var repo in repositories)
        {
            var allFindings = repo.SecretScans
                .SelectMany(s => s.Findings)
                .ToList();

            var crit = allFindings.Count(f => f.Severity == "Critical");
            var high = allFindings.Count(f => f.Severity == "High");
            var med = allFindings.Count(f => f.Severity == "Medium");
            var low = allFindings.Count(f => f.Severity == "Low");

            var totalSec = allFindings.Count;
            var riskScore = (crit * 10) + (high * 5) + (med * 2) + (low * 1);

            string riskLevel;
            if (crit > 0 || riskScore >= 20)
                riskLevel = "Critical Risk";
            else if (high > 0 || riskScore >= 10)
                riskLevel = "High Risk";
            else if (med > 0 || riskScore >= 4)
                riskLevel = "Moderate Risk";
            else if (totalSec > 0)
                riskLevel = "Low Risk";
            else
                riskLevel = "Safe";

            riskyRepositories.Add(new AdminRepositoryRiskViewModel
            {
                RepositoryId = repo.Id,
                RepositoryName = repo.Name,
                RepoUrl = repo.RepoUrl,
                Provider = repo.Provider,
                Owner = repo.Owner,
                CreatedByUserName = repo.CreatedByUser?.UserName ?? "Unknown",
                TotalSecrets = totalSec,
                CriticalCount = crit,
                HighCount = high,
                MediumCount = med,
                LowCount = low,
                RiskScore = riskScore,
                RiskLevel = riskLevel,
                LastScannedAt = repo.LastScannedAt,
                TotalScans = repo.SecretScans.Count
            });
        }

        // Sort by scan date and time descending (most recently scanned first), then by risk score
        riskyRepositories = riskyRepositories
            .OrderByDescending(r => r.LastScannedAt ?? DateTime.MinValue)
            .ThenByDescending(r => r.RiskScore)
            .ThenByDescending(r => r.TotalSecrets)
            .ToList();

        // Top detected secret rules
        var topSecretTypes = await _context.SecretFindings
            .Include(f => f.SecretRule)
            .GroupBy(f => new { f.SecretRuleId, RuleName = f.SecretRule != null ? f.SecretRule.Name : "Custom Rule", f.Severity })
            .Select(g => new AdminSecretTypeSummaryViewModel
            {
                RuleName = g.Key.RuleName,
                Severity = g.Key.Severity,
                Count = g.Count()
            })
            .OrderByDescending(t => t.Count)
            .Take(10)
            .ToListAsync();

        var criticalTotal = await _context.SecretFindings.CountAsync(f => f.Severity == "Critical");
        var highTotal = await _context.SecretFindings.CountAsync(f => f.Severity == "High");
        var mediumTotal = await _context.SecretFindings.CountAsync(f => f.Severity == "Medium");
        var lowTotal = await _context.SecretFindings.CountAsync(f => f.Severity == "Low");

        return new AdminReportViewModel
        {
            GeneratedAt = DateTime.UtcNow,
            TotalUsers = totalUsers,
            TotalRepositories = totalRepos,
            TotalScans = totalScans,
            TotalSecretsFound = totalSecrets,
            TotalFilesScanned = totalFiles,
            TotalScanDurationSeconds = totalDurationSec,
            AverageScanDurationSeconds = avgDurationSec,
            CompletedScansCount = completedScans.Count,
            FailedScansCount = failedScansCount,
            RunningScansCount = runningScansCount,
            RiskyRepositories = riskyRepositories,
            TopSecretTypes = topSecretTypes,
            CriticalSecretsCount = criticalTotal,
            HighSecretsCount = highTotal,
            MediumSecretsCount = mediumTotal,
            LowSecretsCount = lowTotal
        };
    }

    private bool IsAdminAuthenticated(out int userId)
    {
        userId = 0;
        if (User.Identity?.IsAuthenticated != true || !User.IsInRole("Admin"))
        {
            return false;
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out userId);
    }
}