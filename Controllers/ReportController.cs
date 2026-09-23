using Deployguard.Services.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Deployguard.Controllers;

[Authorize]
public class ReportController : Controller
{
private readonly ReportService _reportService;

public ReportController(ReportService reportService)
{
    _reportService = reportService;
}

[HttpGet]
public async Task<IActionResult> Index(int repositoryId)
{
    var userIdClaim = User.FindFirstValue(
        ClaimTypes.NameIdentifier);

    if (!int.TryParse(userIdClaim, out var userId))
    {
        return Forbid();
    }

    var report = await _reportService.GenerateReportAsync(
        repositoryId,
        userId);

    if (report == null)
    {
        return NotFound();
    }

    return View(report);
}

}