using System.Security.Claims;
using Deployguard.Services;
using Microsoft.AspNetCore.Authorization;
using Deployguard.Services.Security;
using Microsoft.AspNetCore.Mvc;

namespace Deployguard.Controllers;

[Authorize]
public class RepositoryController : Controller
{
    private readonly RepositoryService _repositoryService;
    private readonly SecretScanService _scanService;

    public RepositoryController(
        RepositoryService repositoryService,
        SecretScanService scanService)
    {
        _repositoryService = repositoryService;
        _scanService = scanService;
    }

    // GET: /Repository
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Challenge();
        }

        var repositories =
            await _repositoryService.GetUserRepositoriesAsync(
                userId.Value);

        return View(repositories);
    }

    // GET: /Repository/Create
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    // POST: /Repository/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string repoUrl,
        string name)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Challenge();
        }

        try
        {
            var repository =
                await _repositoryService.AddRepositoryAsync(
                    userId.Value,
                    name,
                    repoUrl);

            return RedirectToAction(
                nameof(Details),
                new { id = repository.Id });
        }
        catch (ArgumentException ex)
        {
            ViewBag.Error = ex.Message;
            return View();
        }
        catch (InvalidOperationException ex)
        {
            ViewBag.Error = ex.Message;
            return View();
        }
    }

    // GET: /Repository/Details/5
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Challenge();
        }

        var isAdmin = User.IsInRole("Admin");

        var repository = isAdmin
            ? await _repositoryService.GetRepositoryByIdAsync(id)
            : await _repositoryService.GetUserRepositoryAsync(userId.Value, id);

        if (repository == null)
        {
            return NotFound();
        }

        var latestScan = isAdmin
            ? await _scanService.GetLatestScanAsync(id, repository.CreatedByUserId)
            : await _scanService.GetLatestScanAsync(id, userId.Value);

        ViewBag.LatestScan = latestScan;

        return View(repository);
    }

    // GET: /Repository/Edit/5
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Challenge();
        }

        var repository =
            await _repositoryService.GetUserRepositoryAsync(
                userId.Value,
                id);

        if (repository == null)
        {
            return NotFound();
        }

        return View(repository);
    }

    // POST: /Repository/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        string name,
        string repoUrl)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Challenge();
        }

        try
        {
            var success =
                await _repositoryService.UpdateRepositoryAsync(
                    userId.Value,
                    id,
                    name,
                    repoUrl);

            if (!success)
            {
                return NotFound();
            }

            return RedirectToAction(
                nameof(Details),
                new { id });
        }
        catch (ArgumentException ex)
        {
            ViewBag.Error = ex.Message;

            var repository =
                await _repositoryService.GetUserRepositoryAsync(
                    userId.Value,
                    id);

            if (repository == null)
            {
                return NotFound();
            }

            return View(repository);
        }
        catch (InvalidOperationException ex)
        {
            ViewBag.Error = ex.Message;

            var repository =
                await _repositoryService.GetUserRepositoryAsync(
                    userId.Value,
                    id);

            if (repository == null)
            {
                return NotFound();
            }

            return View(repository);
        }
    }

    // POST: /Repository/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Challenge();
        }

        var success =
            await _repositoryService.DeleteRepositoryAsync(
                userId.Value,
                id);

        if (!success)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: /Repository/RefreshMetadata/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RefreshMetadata(int id)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Challenge();
        }

        var success =
            await _repositoryService.RefreshMetadataAsync(
                userId.Value,
                id);

        if (!success)
        {
            return NotFound();
        }

        return RedirectToAction(
            nameof(Details),
            new { id });
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (int.TryParse(
                userIdClaim,
                out var userId))
        {
            return userId;
        }

        return null;
    }
}