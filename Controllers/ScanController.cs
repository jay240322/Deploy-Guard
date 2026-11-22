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

        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                      Request.Headers.Accept.ToString().Contains("application/json");

        try
        {
            var scan = await _scanService.StartScanAsync(
                repositoryId,
                userId);

            if (isAjax)
            {
                return Json(new
                {
                    success = true,
                    redirectUrl = Url.Action(nameof(Details), new { id = scan.Id })
                });
            }

            return RedirectToAction(
                nameof(Details),
                new { id = scan.Id });
        }
        catch (PrivateRepositoryException ex)
        {
            if (isAjax)
            {
                return Json(new
                {
                    success = false,
                    isPrivate = true,
                    message = ex.Message
                });
            }

            TempData["PrivateRepoError"] = ex.Message;
            return RedirectToAction("Details", "Repository", new { id = repositoryId });
        }
        catch (Exception ex)
        {
            var errorMsg = ex.Message;
            bool looksPrivate = RepositoryCloneService.IsPrivateRepositoryError(errorMsg);

            if (isAjax)
            {
                return Json(new
                {
                    success = false,
                    isPrivate = looksPrivate,
                    message = looksPrivate
                        ? "This repository is private or requires authentication credentials. DeployGuard can only scan publicly accessible repositories."
                        : $"Scan failed: {errorMsg}"
                });
            }

            if (looksPrivate)
            {
                TempData["PrivateRepoError"] = "This repository is private or requires authentication credentials. DeployGuard can only scan publicly accessible repositories.";
            }
            else
            {
                TempData["ErrorMessage"] = $"Scan failed: {errorMsg}";
            }

            return RedirectToAction("Details", "Repository", new { id = repositoryId });
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