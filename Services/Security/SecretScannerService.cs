using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Deployguard.Data;
using Deployguard.Models;
using Microsoft.EntityFrameworkCore;

namespace Deployguard.Services.Security;

public class SecretScannerService
{
    private readonly ApplicationDbContext _context;

    private static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        "node_modules",
        "bin",
        "obj",
        "vendor",
        "dist",
        "build",
        "out",
        "target",
        ".vs",
        ".idea",
        ".vscode",
        "packages",
        ".gradle",
        ".next",
        ".nuxt",
        ".cache",
        "coverage",
        ".terraform",
        "__pycache__",
        ".pytest_cache"
    };

    private static readonly HashSet<string> IgnoredFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "package-lock.json",
        "yarn.lock",
        "pnpm-lock.yaml",
        "composer.lock",
        "cargo.lock",
        "gemfile.lock",
        "poetry.lock",
        "go.sum",
        "packages.lock.json"
    };

    private static readonly HashSet<string> BinaryExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".ico", ".webp",
        ".pdf", ".zip", ".rar", ".7z", ".gz", ".tar",
        ".exe", ".dll", ".so", ".bin", ".dylib",
        ".woff", ".woff2", ".ttf", ".otf", ".eot",
        ".mp3", ".mp4", ".avi", ".mov", ".wav",
        ".svg", ".min.js", ".min.css", ".map"
    };

    public SecretScannerService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SecretScanResult> ScanAsync(string repositoryPath)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
        {
            throw new ArgumentException("Repository path is required.");
        }

        if (!Directory.Exists(repositoryPath))
        {
            throw new DirectoryNotFoundException("Repository directory was not found.");
        }

        var rules = await _context.SecretRules
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.Id)
            .ToListAsync();

        // Precompile regex rules once with safe timeouts
        var compiledRules = rules
            .Where(r => !string.IsNullOrWhiteSpace(r.Pattern))
            .Select(r =>
            {
                try
                {
                    var regex = new Regex(
                        r.Pattern,
                        RegexOptions.Compiled |
                        RegexOptions.IgnoreCase |
                        RegexOptions.CultureInvariant,
                        TimeSpan.FromMilliseconds(200));
                    return (Rule: r, Regex: (Regex?)regex);
                }
                catch
                {
                    return (Rule: r, Regex: (Regex?)null);
                }
            })
            .Where(x => x.Regex != null)
            .Select(x => (x.Rule, Regex: x.Regex!))
            .ToList();

        var candidateFiles = CollectCandidateFiles(repositoryPath);
        var findings = new ConcurrentBag<SecretFindingResult>();
        int filesScanned = 0;

        // Fast parallel file scanning across CPU cores
        await Parallel.ForEachAsync(candidateFiles, new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(2, Environment.ProcessorCount)
        }, async (filePath, ct) =>
        {
            Interlocked.Increment(ref filesScanned);
            await ScanSingleFileAsync(repositoryPath, filePath, compiledRules, findings);
        });

        var result = new SecretScanResult
        {
            FilesScanned = filesScanned,
            Findings = findings
                .OrderBy(f => f.FilePath)
                .ThenBy(f => f.LineNumber)
                .ToList()
        };

        result.SecretsFound = result.Findings.Count;

        return result;
    }

    private static List<string> CollectCandidateFiles(string rootDirectory)
    {
        var filesToScan = new List<string>();
        var directoriesToVisit = new Stack<string>();
        directoriesToVisit.Push(rootDirectory);

        while (directoriesToVisit.Count > 0)
        {
            var currentDir = directoriesToVisit.Pop();

            try
            {
                foreach (var subDir in Directory.EnumerateDirectories(currentDir))
                {
                    var dirName = Path.GetFileName(subDir);
                    if (!IgnoredDirectories.Contains(dirName))
                    {
                        directoriesToVisit.Push(subDir);
                    }
                }
            }
            catch
            {
                // Ignore inaccessible directories
            }

            try
            {
                foreach (var file in Directory.EnumerateFiles(currentDir))
                {
                    var fileName = Path.GetFileName(file);
                    if (IgnoredFileNames.Contains(fileName))
                    {
                        continue;
                    }

                    if (fileName.EndsWith(".min.js", StringComparison.OrdinalIgnoreCase) ||
                        fileName.EndsWith(".min.css", StringComparison.OrdinalIgnoreCase) ||
                        fileName.EndsWith(".map", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var ext = Path.GetExtension(file);
                    if (BinaryExtensions.Contains(ext))
                    {
                        continue;
                    }

                    try
                    {
                        var info = new FileInfo(file);
                        // Skip files larger than 1 MB (e.g. data dumps, bundled assets)
                        if (info.Length > 1024 * 1024)
                        {
                            continue;
                        }
                    }
                    catch
                    {
                        continue;
                    }

                    filesToScan.Add(file);
                }
            }
            catch
            {
                // Ignore inaccessible files
            }
        }

        return filesToScan;
    }

    private static async Task ScanSingleFileAsync(
        string repositoryPath,
        string filePath,
        IReadOnlyList<(SecretRule Rule, Regex Regex)> compiledRules,
        ConcurrentBag<SecretFindingResult> findings)
    {
        string[] lines;

        try
        {
            lines = await File.ReadAllLinesAsync(filePath);
        }
        catch
        {
            return;
        }

        var relativePath = Path.GetRelativePath(repositoryPath, filePath).Replace('\\', '/');

        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];

            if (string.IsNullOrWhiteSpace(line) || line.Length > 1000)
            {
                // Skip empty or excessively long lines (e.g. minified single-line code)
                continue;
            }

            foreach (var (rule, regex) in compiledRules)
            {
                IEnumerable<Match> matches;

                try
                {
                    matches = regex.Matches(line).Cast<Match>();
                }
                catch
                {
                    continue;
                }

                foreach (var match in matches)
                {
                    if (!match.Success || string.IsNullOrWhiteSpace(match.Value))
                    {
                        continue;
                    }

                    var masked = MaskSecret(match.Value);

                    findings.Add(new SecretFindingResult
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

    private static string MaskSecret(string secret)
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

    public List<SecretFindingResult> Findings { get; set; } = new();
}

public class SecretFindingResult
{
    public int SecretRuleId { get; set; }

    public string FilePath { get; set; } = string.Empty;

    public int LineNumber { get; set; }

    public string MaskedValue { get; set; } = string.Empty;

    public string Severity { get; set; } = string.Empty;
}
