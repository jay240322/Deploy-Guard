using Deployguard.Data;
using Deployguard.Models;
using Deployguard.Services.Providers;
using Microsoft.EntityFrameworkCore;

namespace Deployguard.Services;

public class RepositoryService
{
    private readonly ApplicationDbContext _context;
    private readonly RepositoryUrlParser _urlParser;
    private readonly IReadOnlyDictionary<string, IGitProvider> _providers;

    public RepositoryService(
        ApplicationDbContext context,
        RepositoryUrlParser urlParser,
        IEnumerable<IGitProvider> providers)
    {
        _context = context;
        _urlParser = urlParser;

        _providers = providers.ToDictionary(
            provider => provider.ProviderName,
            StringComparer.OrdinalIgnoreCase);
    }

    public async Task<Repository> AddRepositoryAsync(
        int userId,
        string name,
        string repoUrl)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Repository name is required.");
        }

        if (string.IsNullOrWhiteSpace(repoUrl))
        {
            throw new ArgumentException(
                "Repository URL is required.");
        }

        name = name.Trim();
        repoUrl = repoUrl.Trim();

        var repositoryInfo = _urlParser.Parse(repoUrl);

        var alreadyExists = await _context.Repositories
            .AnyAsync(r =>
                r.CreatedByUserId == userId &&
                r.RepoUrl == repoUrl);

        if (alreadyExists)
        {
            throw new InvalidOperationException(
                "You have already added this repository.");
        }

        var repository = new Repository
        {
            Name = name,
            RepoUrl = repoUrl,

            Provider = repositoryInfo.Provider,
            Owner = repositoryInfo.Owner,
            RepoName = repositoryInfo.RepoName,

            DefaultBranch = string.Empty,
            Language = string.Empty,
            SizeKb = 0,

            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        await ApplyMetadataAsync(repository);

        _context.Repositories.Add(repository);

        await _context.SaveChangesAsync();

        return repository;
    }

    public async Task<List<Repository>> GetUserRepositoriesAsync(
        int userId)
    {
        return await _context.Repositories
            .Where(r => r.CreatedByUserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<Repository?> GetUserRepositoryAsync(
        int userId,
        int repositoryId)
    {
        return await _context.Repositories
            .FirstOrDefaultAsync(r =>
                r.Id == repositoryId &&
                r.CreatedByUserId == userId);
    }

    public async Task<Repository?> GetRepositoryByIdAsync(
        int repositoryId)
    {
        return await _context.Repositories
            .FirstOrDefaultAsync(r => r.Id == repositoryId);
    }

    public async Task<bool> RefreshMetadataAsync(
        int userId,
        int repositoryId)
    {
        var repository = await _context.Repositories
            .FirstOrDefaultAsync(r =>
                r.Id == repositoryId &&
                r.CreatedByUserId == userId);

        if (repository == null)
        {
            return false;
        }

        var metadataFound = await ApplyMetadataAsync(repository);

        if (!metadataFound)
        {
            return false;
        }

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdateRepositoryAsync(
        int userId,
        int repositoryId,
        string name,
        string repoUrl)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Repository name is required.");
        }

        if (string.IsNullOrWhiteSpace(repoUrl))
        {
            throw new ArgumentException(
                "Repository URL is required.");
        }

        var repository = await _context.Repositories
            .FirstOrDefaultAsync(r =>
                r.Id == repositoryId &&
                r.CreatedByUserId == userId);

        if (repository == null)
        {
            return false;
        }

        name = name.Trim();
        repoUrl = repoUrl.Trim();

        var repositoryInfo = _urlParser.Parse(repoUrl);

        var duplicateExists = await _context.Repositories
            .AnyAsync(r =>
                r.Id != repositoryId &&
                r.CreatedByUserId == userId &&
                r.RepoUrl == repoUrl);

        if (duplicateExists)
        {
            throw new InvalidOperationException(
                "You have already added this repository.");
        }

        repository.Name = name;
        repository.RepoUrl = repoUrl;

        repository.Provider = repositoryInfo.Provider;
        repository.Owner = repositoryInfo.Owner;
        repository.RepoName = repositoryInfo.RepoName;

        repository.DefaultBranch = string.Empty;
        repository.Language = string.Empty;
        repository.SizeKb = 0;

        await ApplyMetadataAsync(repository);

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteRepositoryAsync(
        int userId,
        int repositoryId)
    {
        var repository = await _context.Repositories
            .FirstOrDefaultAsync(r =>
                r.Id == repositoryId &&
                r.CreatedByUserId == userId);

        if (repository == null)
        {
            return false;
        }

        _context.Repositories.Remove(repository);

        await _context.SaveChangesAsync();

        return true;
    }

    private async Task<bool> ApplyMetadataAsync(
        Repository repository)
    {
        if (!_providers.TryGetValue(
                repository.Provider,
                out var provider))
        {
            return false;
        }

        var metadata = await provider.GetMetadataAsync(
            repository.Owner,
            repository.RepoName);

        if (metadata == null)
        {
            return false;
        }

        repository.DefaultBranch =
            metadata.DefaultBranch;

        repository.Language =
            metadata.Language;

        repository.SizeKb =
            metadata.SizeKb;

        return true;
    }
}