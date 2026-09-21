using Deployguard.Data;
using Deployguard.Models;
using Deployguard.Services.Providers;
using Microsoft.EntityFrameworkCore;

namespace Deployguard.Services;

public class RepositoryService
{
    private readonly ApplicationDbContext _context;
    private readonly RepositoryUrlParser _urlParser;
    private readonly GitHubProvider _gitHubProvider;

    public RepositoryService(
        ApplicationDbContext context,
        RepositoryUrlParser urlParser,
        GitHubProvider gitHubProvider)
    {
        _context = context;
        _urlParser = urlParser;
        _gitHubProvider = gitHubProvider;
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

        // Parse repository URL
        var repositoryInfo = _urlParser.Parse(repoUrl);

        // Check duplicate repository
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

        // Fetch GitHub metadata
        if (repositoryInfo.Provider == "GitHub")
        {
            var metadata =
                await _gitHubProvider.GetMetadataAsync(
                    repositoryInfo.Owner,
                    repositoryInfo.RepoName);

            if (metadata != null)
            {
                repository.DefaultBranch =
                    metadata.DefaultBranch;

                repository.Language =
                    metadata.Language;

                repository.SizeKb =
                    metadata.SizeKb;
            }
        }

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

    if (repository.Provider == "GitHub")
    {
        var metadata =
            await _gitHubProvider.GetMetadataAsync(
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
    }

    await _context.SaveChangesAsync();

    return true;
}
}