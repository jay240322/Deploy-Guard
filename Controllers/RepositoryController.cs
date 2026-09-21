using System.Security.Claims;
using Deployguard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Deployguard.Controllers;

[Authorize]
public class RepositoryController : Controller
{
    private readonly RepositoryService _repositoryService;

    public RepositoryController(RepositoryService repositoryService)
    {
        _repositoryService = repositoryService;
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

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return null;
    }
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

        if (string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(repoUrl))
        {
            ViewBag.Error =
                "Repository name and URL are required.";

            var existingRepository =
                await _repositoryService.GetUserRepositoryAsync(
                    userId.Value,
                    id);

            if (existingRepository == null)
            {
                return NotFound();
            }

            return View(existingRepository);
        }

        var repository =
            await _repositoryService.GetUserRepositoryAsync(
                userId.Value,
                id);

        if (repository == null)
        {
            return NotFound();
        }

        repository.Name = name.Trim();

        // If URL changes, re-parse the provider information.
        if (!repository.RepoUrl.Equals(
                repoUrl.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            var parser = new RepositoryUrlParser();

            try
            {
                var repositoryInfo =
                    parser.Parse(repoUrl.Trim());

                repository.RepoUrl = repoUrl.Trim();
                repository.Provider = repositoryInfo.Provider;
                repository.Owner = repositoryInfo.Owner;
                repository.RepoName = repositoryInfo.RepoName;

                // Clear metadata because the repository changed.
                repository.DefaultBranch = string.Empty;
                repository.Language = string.Empty;
                repository.SizeKb = 0;
            }
            catch (ArgumentException ex)
            {
                ViewBag.Error = ex.Message;
                return View(repository);
            }
        }

        await HttpContext.RequestServices
            .GetRequiredService<Deployguard.Data.ApplicationDbContext>()
            .SaveChangesAsync();

        return RedirectToAction(
            nameof(Details),
            new { id });
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

        var repository =
            await _repositoryService.GetUserRepositoryAsync(
                userId.Value,
                id);

        if (repository == null)
        {
            return NotFound();
        }

        var context =
            HttpContext.RequestServices
                .GetRequiredService<Deployguard.Data.ApplicationDbContext>();

        context.Repositories.Remove(repository);

        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}