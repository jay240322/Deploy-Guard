using System.Text.Json;
using Deployguard.Data;
using Deployguard.Models;
using Microsoft.EntityFrameworkCore;

namespace Deployguard.Services.Pipeline;

public class PipelineDetectionService
{
    private readonly ApplicationDbContext _context;
    private readonly PipelineParserService _parserService;

    public PipelineDetectionService(
        ApplicationDbContext context,
        PipelineParserService parserService)
    {
        _context = context;
        _parserService = parserService;
    }

    public async Task<List<PipelineFile>> DetectAsync(
        int repositoryId,
        string repositoryPath)
    {
        var pipelineFiles = new List<PipelineFile>();

        // GitHub Actions
        var githubWorkflowDirectory = Path.Combine(
            repositoryPath,
            ".github",
            "workflows");

        if (Directory.Exists(githubWorkflowDirectory))
        {
            var files = Directory.GetFiles(
                githubWorkflowDirectory,
                "*.*",
                SearchOption.TopDirectoryOnly);

            foreach (var file in files)
            {
                if (!IsYamlFile(file))
                {
                    continue;
                }

                var pipelineFile = await CreatePipelineFileAsync(
                    repositoryId,
                    repositoryPath,
                    file,
                    "GitHub Actions");

                pipelineFiles.Add(pipelineFile);
            }
        }

        // GitLab CI
        var gitlabFile = Path.Combine(
            repositoryPath,
            ".gitlab-ci.yml");

        if (File.Exists(gitlabFile))
        {
            var pipelineFile = await CreatePipelineFileAsync(
                repositoryId,
                repositoryPath,
                gitlabFile,
                "GitLab CI");

            pipelineFiles.Add(pipelineFile);
        }

        // Jenkins
        var jenkinsFile = Path.Combine(
            repositoryPath,
            "Jenkinsfile");

        if (File.Exists(jenkinsFile))
        {
            var pipelineFile = await CreatePipelineFileAsync(
                repositoryId,
                repositoryPath,
                jenkinsFile,
                "Jenkins");

            pipelineFiles.Add(pipelineFile);
        }

        // Azure Pipelines
        var azureFile = Path.Combine(
            repositoryPath,
            "azure-pipelines.yml");

        if (File.Exists(azureFile))
        {
            var pipelineFile = await CreatePipelineFileAsync(
                repositoryId,
                repositoryPath,
                azureFile,
                "Azure Pipelines");

            pipelineFiles.Add(pipelineFile);
        }

        // CircleCI
        var circleFile = Path.Combine(
            repositoryPath,
            ".circleci",
            "config.yml");

        if (File.Exists(circleFile))
        {
            var pipelineFile = await CreatePipelineFileAsync(
                repositoryId,
                repositoryPath,
                circleFile,
                "CircleCI");

            pipelineFiles.Add(pipelineFile);
        }

        // Bitbucket Pipelines
        var bitbucketFile = Path.Combine(
            repositoryPath,
            "bitbucket-pipelines.yml");

        if (File.Exists(bitbucketFile))
        {
            var pipelineFile = await CreatePipelineFileAsync(
                repositoryId,
                repositoryPath,
                bitbucketFile,
                "Bitbucket Pipelines");

            pipelineFiles.Add(pipelineFile);
        }

        return pipelineFiles;
    }

    private async Task<PipelineFile> CreatePipelineFileAsync(
        int repositoryId,
        string repositoryPath,
        string fullPath,
        string provider)
    {
        var rawContent = await File.ReadAllTextAsync(fullPath);

        var relativePath = Path.GetRelativePath(
                repositoryPath,
                fullPath)
            .Replace("\\", "/");

        // Check whether this pipeline file already exists
        var pipelineFile = await _context.PipelineFiles
            .Include(p => p.Stages)
            .FirstOrDefaultAsync(p =>
                p.RepositoryId == repositoryId &&
                p.FilePath == relativePath);

        if (pipelineFile == null)
        {
            // Create new pipeline file
            pipelineFile = new PipelineFile
            {
                RepositoryId = repositoryId,
                FilePath = relativePath,
                Provider = provider,
                RawYaml = rawContent,
                ParsedJson = string.Empty,
                DetectedAt = DateTime.UtcNow
            };

            _context.PipelineFiles.Add(pipelineFile);

            // Save first so PipelineFile gets its ID
            await _context.SaveChangesAsync();
        }
        else
        {
            // Update existing pipeline file
            pipelineFile.Provider = provider;
            pipelineFile.RawYaml = rawContent;
            pipelineFile.ParsedJson = string.Empty;
            pipelineFile.DetectedAt = DateTime.UtcNow;

            // Remove previously parsed stages
            if (pipelineFile.Stages.Any())
            {
                _context.PipelineStages.RemoveRange(
                    pipelineFile.Stages);
            }

            await _context.SaveChangesAsync();
        }

        // Parse pipeline stages
        var stages = _parserService.Parse(
            pipelineFile);

        // Save newly parsed stages
        foreach (var stage in stages)
        {
            _context.PipelineStages.Add(stage);
        }

        // Store parsed stage information as JSON
        pipelineFile.ParsedJson =
            JsonSerializer.Serialize(
                stages.Select(stage => new
                {
                    stage.Name,
                    stage.Order,
                    stage.DependsOn,
                    stage.IsParallel,
                    stage.Condition,
                    stage.EstimatedDurationSec
                }));

        await _context.SaveChangesAsync();

        return pipelineFile;
    }

    private static bool IsYamlFile(
        string filePath)
    {
        var extension = Path.GetExtension(filePath);

        return extension.Equals(
                   ".yml",
                   StringComparison.OrdinalIgnoreCase)
               ||
               extension.Equals(
                   ".yaml",
                   StringComparison.OrdinalIgnoreCase);
    }
}