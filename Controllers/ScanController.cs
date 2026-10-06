using Deployguard.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Deployguard.Controllers;

[Authorize]
public class ScanController : Controller
{
    private readonly SecretScanService _scanService;

    public ScanController(
        SecretScanService scanService)
    {
        _scanService = scanService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(
        int repositoryId)
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(
                userIdClaim,
                out var userId))
        {
            return Forbid();
        }

        try
        {
            var scan = await _scanService.StartScanAsync(
                repositoryId,
                userId);

            return RedirectToAction(
                nameof(Details),
                new { id = scan.Id });
        }
        catch (Exception ex)
        {
            return Content(
                $"SCAN ERROR:\n\n{ex}");
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(
        int id)
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(
                userIdClaim,
                out var userId))
        {
            return Forbid();
        }

        var scan = User.IsInRole("Admin")
            ? await _scanService.GetScanByIdAsync(id)
            : await _scanService.GetScanAsync(id, userId);

        if (scan == null)
        {
            return NotFound();
        }

        return View(scan);
    }

    [HttpGet]
    public async Task<IActionResult> History()
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(
                userIdClaim,
                out var userId))
        {
            return Forbid();
        }

        var scans = User.IsInRole("Admin")
            ? await _scanService.GetAllScansAsync()
            : await _scanService.GetUserScansAsync(userId);

        return View(scans);
    }
}