using Deployguard.Data;
using Deployguard.Models;
using Microsoft.EntityFrameworkCore;

namespace Deployguard.Services.Reports;

public class ReportService
{
    private readonly ApplicationDbContext _context;

    public ReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReportViewModel?> GenerateReportAsync(
        int repositoryId,
        int userId)
    {
        // Get repository and verify ownership
        var repository = await _context.Repositories
            .FirstOrDefaultAsync(r =>
                r.Id == repositoryId &&
                r.CreatedByUserId == userId);

        if (repository == null)
        {
            return null;
        }

        // Get latest security scan
        var latestScan = await _context.SecretScans
            .Include(s => s.Findings)
                .ThenInclude(f => f.SecretRule)
            .Where(s =>
                s.RepositoryId == repositoryId &&
                s.TriggeredByUserId == userId)
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefaultAsync();

        // Get pipeline files
        var pipelineFiles = await _context.PipelineFiles
            .Include(p => p.Stages)
            .Where(p => p.RepositoryId == repositoryId)
            .OrderByDescending(p => p.DetectedAt)
            .ToListAsync();

        // Get all pipeline stages
        var pipelineStages = pipelineFiles
            .SelectMany(p => p.Stages)
            .OrderBy(s => s.Order)
            .ToList();

        // Get findings from latest scan
        var findings = latestScan?.Findings
            .ToList()
            ?? new List<SecretFinding>();

        // Build combined report
        return new ReportViewModel
        {
            // Repository information
            RepositoryId = repository.Id,
            RepositoryName = repository.Name,
            RepositoryUrl = repository.RepoUrl,
            Provider = repository.Provider,

            // Scan information
            ScanId = latestScan?.Id ?? 0,
            ScanStartedAt = latestScan?.StartedAt,
            ScanCompletedAt = latestScan?.CompletedAt,
            ScanStatus = latestScan?.Status ?? "No Scan",

            // Security summary
            FilesScanned = latestScan?.FilesScanned ?? 0,
            SecretsFound = latestScan?.SecretsFound ?? 0,

            CriticalCount = findings.Count(f =>
                string.Equals(
                    f.Severity,
                    "Critical",
                    StringComparison.OrdinalIgnoreCase)),

            HighCount = findings.Count(f =>
                string.Equals(
                    f.Severity,
                    "High",
                    StringComparison.OrdinalIgnoreCase)),

            MediumCount = findings.Count(f =>
                string.Equals(
                    f.Severity,
                    "Medium",
                    StringComparison.OrdinalIgnoreCase)),

            LowCount = findings.Count(f =>
                string.Equals(
                    f.Severity,
                    "Low",
                    StringComparison.OrdinalIgnoreCase)),

            Findings = findings,

            // Pipeline summary
            PipelineFileCount = pipelineFiles.Count,
            PipelineStageCount = pipelineStages.Count,

            PipelineFiles = pipelineFiles,
            PipelineStages = pipelineStages,

            // Report metadata
            GeneratedAt = DateTime.UtcNow
        };
    }
}