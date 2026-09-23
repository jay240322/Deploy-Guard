using Deployguard.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Deployguard.Controllers;

[Authorize]
public class PipelineController : Controller
{
    private readonly ApplicationDbContext _context;

    public PipelineController(
        ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
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

        // Verify repository belongs to current user
        var repository = await _context.Repositories
            .FirstOrDefaultAsync(r =>
                r.Id == repositoryId &&
                r.CreatedByUserId == userId);

        if (repository == null)
        {
            return NotFound();
        }

        var pipelineFiles = await _context.PipelineFiles
            .Include(p => p.Stages)
            .Where(p =>
                p.RepositoryId == repositoryId)
            .OrderByDescending(p => p.DetectedAt)
            .ToListAsync();

        ViewBag.Repository = repository;

        return View(pipelineFiles);
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

        var pipelineFile = await _context.PipelineFiles
            .Include(p => p.Repository)
            .Include(p => p.Stages
                .OrderBy(s => s.Order))
            .FirstOrDefaultAsync(p =>
                p.Id == id &&
                p.Repository != null &&
                p.Repository.CreatedByUserId == userId);

        if (pipelineFile == null)
        {
            return NotFound();
        }

        return View(pipelineFile);
    }
}