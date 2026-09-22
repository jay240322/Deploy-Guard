using System.Diagnostics;

namespace Deployguard.Services.Security;

public class RepositoryCloneService
{
    private readonly IWebHostEnvironment _environment;

    public RepositoryCloneService(
        IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> CloneAsync(
        string repoUrl,
        int scanId)
    {
        if (string.IsNullOrWhiteSpace(repoUrl))
        {
            throw new ArgumentException(
                "Repository URL is required.");
        }

        var scanRoot = Path.Combine(
            _environment.ContentRootPath,
            "App_Data",
            "Scans");

        Directory.CreateDirectory(scanRoot);

        var repositoryPath = Path.Combine(
            scanRoot,
            $"scan-{scanId}");

        if (Directory.Exists(repositoryPath))
        {
            DeleteClone(repositoryPath);
        }

        var arguments =
            $"clone --depth 1 \"{repoUrl}\" \"{repositoryPath}\"";

        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = scanRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process =
            new Process
            {
                StartInfo = startInfo
            };

        process.Start();

        var outputTask =
            process.StandardOutput.ReadToEndAsync();

        var errorTask =
            process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Repository clone failed: {error}");
        }

        if (!Directory.Exists(repositoryPath))
        {
            throw new InvalidOperationException(
                "Repository was not cloned successfully.");
        }

        return repositoryPath;
    }

    public void DeleteClone(string repositoryPath)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
        {
            return;
        }

        if (!Directory.Exists(repositoryPath))
        {
            return;
        }

        try
        {
            // Git files can sometimes be marked read-only.
            RemoveReadOnlyAttributes(repositoryPath);

            Directory.Delete(
                repositoryPath,
                recursive: true);
        }
        catch (UnauthorizedAccessException)
        {
            // Give Windows a moment to release Git files.
            Thread.Sleep(500);

            try
            {
                RemoveReadOnlyAttributes(repositoryPath);

                Directory.Delete(
                    repositoryPath,
                    recursive: true);
            }
            catch
            {
                // Cleanup failure should not make
                // an otherwise successful scan fail.
            }
        }
        catch (IOException)
        {
            // Cleanup failure should not make
            // an otherwise successful scan fail.
        }
    }

    private static void RemoveReadOnlyAttributes(
        string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(
                     directoryPath,
                     "*",
                     SearchOption.AllDirectories))
        {
            try
            {
                File.SetAttributes(
                    file,
                    FileAttributes.Normal);
            }
            catch
            {
                // Ignore files that cannot have
                // their attributes changed.
            }
        }

        foreach (var directory in Directory.EnumerateDirectories(
                     directoryPath,
                     "*",
                     SearchOption.AllDirectories))
        {
            try
            {
                File.SetAttributes(
                    directory,
                    FileAttributes.Normal);
            }
            catch
            {
                // Ignore directories that cannot have
                // their attributes changed.
            }
        }

        try
        {
            File.SetAttributes(
                directoryPath,
                FileAttributes.Normal);
        }
        catch
        {
            // Ignore attribute errors.
        }
    }
}