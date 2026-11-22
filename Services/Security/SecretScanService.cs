using Deployguard.Data;
using Deployguard.Models;
using Deployguard.Services.Pipeline;
using Microsoft.EntityFrameworkCore;

namespace Deployguard.Services.Security;

public class SecretScanService
{
    private readonly ApplicationDbContext _context;
    private readonly RepositoryCloneService _cloneService;
    private readonly SecretScannerService _scannerService;
    private readonly PipelineDetectionService _pipelineDetectionService;

    public SecretScanService(
        ApplicationDbContext context,
        RepositoryCloneService cloneService,
        SecretScannerService scannerService,
        PipelineDetectionService pipelineDetectionService)
    {
        _context = context;
        _cloneService = cloneService;
        _scannerService = scannerService;
        _pipelineDetectionService = pipelineDetectionService;
    }

    public async Task<SecretScan> StartScanAsync(
        int repositoryId,
        int userId)
    {
        // 1. Verify repository ownership
        var repository = await _context.Repositories
            .FirstOrDefaultAsync(r =>
                r.Id == repositoryId &&
                r.CreatedByUserId == userId);

        if (repository == null)
        {
            throw new InvalidOperationException(
                "Repository was not found or you do not have access to it.");
        }

        // 2. Create scan record
        var scan = new SecretScan
        {
            RepositoryId = repository.Id,
            StartedAt = DateTime.UtcNow,
            Status = "Running",
            TriggeredByUserId = userId,
            FilesScanned = 0,
            SecretsFound = 0
        };

        _context.SecretScans.Add(scan);

        await _context.SaveChangesAsync();

        string? repositoryPath = null;

        try
        {
            // 3. Clone repository
            repositoryPath = await _cloneService.CloneAsync(
                repository.RepoUrl,
                scan.Id);

            // 4. Scan repository for secrets
            var result = await _scannerService.ScanAsync(
                repositoryPath);

            // 5. Detect CI/CD pipeline files
            await _pipelineDetectionService.DetectAsync(
                repository.Id,
                repositoryPath);

            // 6. Save secret findings
            foreach (var finding in result.Findings)
            {
                _context.SecretFindings.Add(
                    new SecretFinding
                    {
                        SecretScanId = scan.Id,
                        SecretRuleId = finding.SecretRuleId,
                        FilePath = finding.FilePath,
                        LineNumber = finding.LineNumber,
                        MaskedValue = finding.MaskedValue,
                        Severity = finding.Severity,
                        IsFalsePositive = false,
                        Remarks = string.Empty
                    });
            }

            // 7. Update scan
            scan.FilesScanned = result.FilesScanned;
            scan.SecretsFound = result.SecretsFound;
            scan.Status = "Completed";
            scan.CompletedAt = DateTime.UtcNow;

            // 8. Update repository
            repository.LastScannedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return scan;
        }
        catch (PrivateRepositoryException)
        {
            try
            {
                _context.SecretScans.Remove(scan);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Ignore cleanup error
            }

            throw;
        }
        catch
        {
            // Mark scan as failed
            scan.Status = "Failed";
            scan.CompletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            throw;
        }
        finally
        {
            // 9. Always remove cloned repository
            if (!string.IsNullOrWhiteSpace(repositoryPath))
            {
                _cloneService.DeleteClone(repositoryPath);
            }
        }
    }

    public async Task<SecretScan?> GetScanAsync(
        int scanId,
        int userId)
    {
        return await _context.SecretScans
            .Include(s => s.Repository)
            .Include(s => s.Findings)
                .ThenInclude(f => f.SecretRule)
            .FirstOrDefaultAsync(s =>
                s.Id == scanId &&
                s.TriggeredByUserId == userId);
    }

    public async Task<SecretScan?> GetScanByIdAsync(
        int scanId)
    {
        return await _context.SecretScans
            .Include(s => s.Repository)
            .Include(s => s.Findings)
                .ThenInclude(f => f.SecretRule)
            .FirstOrDefaultAsync(s =>
                s.Id == scanId);
    }

    public async Task<List<SecretScan>> GetAllScansAsync()
    {
        return await _context.SecretScans
            .Include(s => s.Repository)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync();
    }

    public async Task<List<SecretScan>> GetUserScansAsync(
        int userId)
    {
        return await _context.SecretScans
            .Include(s => s.Repository)
            .Where(s =>
                s.TriggeredByUserId == userId)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync();
    }

    public async Task<SecretScan?> GetLatestScanAsync(
        int repositoryId,
        int userId)
    {
        return await _context.SecretScans
            .Include(s => s.Repository)
            .Include(s => s.Findings)
                .ThenInclude(f => f.SecretRule)
            .Where(s =>
                s.RepositoryId == repositoryId &&
                s.TriggeredByUserId == userId)
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefaultAsync();
    }
}