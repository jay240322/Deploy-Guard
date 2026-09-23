using Deployguard.Services.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Deployguard.Controllers;

[Authorize]
public class ReportController : Controller
{
    private readonly ReportService _reportService;
    private readonly PdfReportService _pdfReportService;

    public ReportController(
        ReportService reportService,
        PdfReportService pdfReportService)
    {
        _reportService = reportService;
        _pdfReportService = pdfReportService;
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

    [HttpGet]
    public async Task<IActionResult> Pdf(int repositoryId)
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

        var pdfBytes = _pdfReportService.Generate(report);

        var fileName =
            $"DeployGuard-Report-{report.RepositoryName}.pdf";

        return File(
            pdfBytes,
            "application/pdf",
            fileName);
    }
}