using Deployguard.Data;
using Deployguard.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Deployguard.Services.Security;

public class SecretScannerService
{
    private readonly ApplicationDbContext _context;

    public SecretScannerService(
        ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SecretScanResult> ScanAsync(
        string repositoryPath)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
        {
            throw new ArgumentException(
                "Repository path is required.");
        }

        if (!Directory.Exists(repositoryPath))
        {
            throw new DirectoryNotFoundException(
                "Repository directory was not found.");
        }

        var rules = await _context.SecretRules
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.Id)
            .ToListAsync();

        var result = new SecretScanResult();

        var files = Directory.EnumerateFiles(
            repositoryPath,
            "*",
            SearchOption.AllDirectories)
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var filePath in files)
        {
            if (ShouldIgnoreFile(filePath))
            {
                continue;
            }

            result.FilesScanned++;

            await ScanFileAsync(
                repositoryPath,
                filePath,
                rules,
                result);
        }

        result.SecretsFound =
            result.Findings.Count;

        return result;
    }

    private async Task ScanFileAsync(
        string repositoryPath,
        string filePath,
        IReadOnlyList<SecretRule> rules,
        SecretScanResult result)
    {
        if (IsBinaryFile(filePath))
        {
            return;
        }

        string[] lines;

        try
        {
            lines = await File.ReadAllLinesAsync(
                filePath);
        }
        catch
        {
            return;
        }

        for (var lineIndex = 0;
             lineIndex < lines.Length;
             lineIndex++)
        {
            var line = lines[lineIndex];

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            foreach (var rule in rules)
            {
                if (string.IsNullOrWhiteSpace(
                        rule.Pattern))
                {
                    continue;
                }

                IEnumerable<Match> matches;

                try
                {
                    matches = Regex
                        .Matches(
                            line,
                            rule.Pattern,
                            RegexOptions.IgnoreCase |
                            RegexOptions.CultureInvariant,
                            TimeSpan.FromSeconds(1))
                        .Cast<Match>();
                }
                catch
                {
                    continue;
                }

                foreach (var match in matches)
                {
                    if (!match.Success ||
                        string.IsNullOrWhiteSpace(
                            match.Value))
                    {
                        continue;
                    }

                    var relativePath =
                        Path.GetRelativePath(
                            repositoryPath,
                            filePath);

                    var masked = MaskSecret(match.Value);

                    var alreadyExists = result.Findings.Any(f =>
                        f.SecretRuleId == rule.Id &&
                        f.FilePath == relativePath &&
                        f.LineNumber == lineIndex + 1 &&
                        f.MaskedValue == masked);

                    if (!alreadyExists)
                    {
                        result.Findings.Add(
                            new SecretFindingResult
                            {
                                SecretRuleId = rule.Id,
                                FilePath = relativePath,
                                LineNumber = lineIndex + 1,
                                MaskedValue = masked,
                                Severity = rule.Severity
                            });
                    }
                }
            }
        }
    }

    private static bool ShouldIgnoreFile(
        string filePath)
    {
        var normalizedPath =
            filePath.Replace('\\', '/');

        return normalizedPath.Contains(
            "/.git/",
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBinaryFile(
        string filePath)
    {
        var extension =
            Path.GetExtension(filePath)
                .ToLowerInvariant();

        var binaryExtensions = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            ".png",
            ".jpg",
            ".jpeg",
            ".gif",
            ".bmp",
            ".ico",
            ".webp",
            ".pdf",
            ".zip",
            ".rar",
            ".7z",
            ".gz",
            ".tar",
            ".exe",
            ".dll",
            ".so",
            ".bin",
            ".woff",
            ".woff2",
            ".ttf",
            ".otf",
            ".mp3",
            ".mp4",
            ".avi",
            ".mov"
        };

        return binaryExtensions.Contains(extension);
    }

    private static string MaskSecret(
        string secret)
    {
        if (secret.Length <= 6)
        {
            return "******";
        }

        if (secret.Length <= 10)
        {
            return secret[..2]
                   + "******"
                   + secret[^2..];
        }

        return secret[..4]
               + "****************"
               + secret[^4..];
    }
}

public class SecretScanResult
{
    public int FilesScanned { get; set; }

    public int SecretsFound { get; set; }

    public List<SecretFindingResult> Findings { get; set; } =
        new();
}

public class SecretFindingResult
{
    public int SecretRuleId { get; set; }

    public string FilePath { get; set; } =
        string.Empty;

    public int LineNumber { get; set; }

    public string MaskedValue { get; set; } =
        string.Empty;

    public string Severity { get; set; } =
        string.Empty;
}