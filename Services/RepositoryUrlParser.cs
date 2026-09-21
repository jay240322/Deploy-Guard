namespace Deployguard.Services;

public class RepositoryUrlParser
{
    public RepositoryUrlInfo Parse(string repoUrl)
    {
        if (string.IsNullOrWhiteSpace(repoUrl))
        {
            throw new ArgumentException(
                "Repository URL is required.");
        }

        if (!Uri.TryCreate(
                repoUrl.Trim(),
                UriKind.Absolute,
                out var uri))
        {
            throw new ArgumentException(
                "Invalid repository URL.");
        }

        if (uri.Scheme != Uri.UriSchemeHttps &&
            uri.Scheme != Uri.UriSchemeHttp)
        {
            throw new ArgumentException(
                "Repository URL must use HTTP or HTTPS.");
        }

        var host = uri.Host.ToLowerInvariant();

        if (host == "github.com" ||
            host == "www.github.com")
        {
            return ParseGitHub(uri);
        }

        if (host == "gitlab.com" ||
            host == "www.gitlab.com")
        {
            return ParseGitLab(uri);
        }

        if (host == "bitbucket.org" ||
            host == "www.bitbucket.org")
        {
            return ParseBitbucket(uri);
        }

        if (host == "dev.azure.com")
        {
            return ParseAzureDevOps(uri);
        }

        throw new ArgumentException(
            "Unsupported Git provider. " +
            "Supported providers are GitHub, GitLab, " +
            "Bitbucket and Azure DevOps.");
    }

    private RepositoryUrlInfo ParseGitHub(Uri uri)
    {
        var segments = GetSegments(uri);

        if (segments.Length < 2)
        {
            throw new ArgumentException(
                "Invalid GitHub repository URL.");
        }

        return new RepositoryUrlInfo
        {
            Provider = "GitHub",
            Owner = segments[0],
            RepoName = RemoveGitExtension(segments[1])
        };
    }

    private RepositoryUrlInfo ParseGitLab(Uri uri)
    {
        var segments = GetSegments(uri);

        if (segments.Length < 2)
        {
            throw new ArgumentException(
                "Invalid GitLab repository URL.");
        }

        // GitLab can support nested groups:
        // group/subgroup/repository
        var repoName = RemoveGitExtension(
            segments[^1]);

        var owner = string.Join(
            "/",
            segments.Take(segments.Length - 1));

        if (string.IsNullOrWhiteSpace(owner))
        {
            throw new ArgumentException(
                "Invalid GitLab repository URL.");
        }

        return new RepositoryUrlInfo
        {
            Provider = "GitLab",
            Owner = owner,
            RepoName = repoName
        };
    }

    private RepositoryUrlInfo ParseBitbucket(Uri uri)
    {
        var segments = GetSegments(uri);

        if (segments.Length < 2)
        {
            throw new ArgumentException(
                "Invalid Bitbucket repository URL.");
        }

        return new RepositoryUrlInfo
        {
            Provider = "Bitbucket",
            Owner = segments[0],
            RepoName = RemoveGitExtension(segments[1])
        };
    }

    private RepositoryUrlInfo ParseAzureDevOps(Uri uri)
    {
        var segments = GetSegments(uri);

        // Expected:
        // dev.azure.com/{organization}/{project}/_git/{repository}

        if (segments.Length < 4 ||
            !segments[2].Equals(
                "_git",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Invalid Azure DevOps repository URL.");
        }

        return new RepositoryUrlInfo
        {
            Provider = "Azure DevOps",
            Owner = segments[0],
            RepoName = RemoveGitExtension(segments[3])
        };
    }

    private static string[] GetSegments(Uri uri)
    {
        return uri.AbsolutePath
            .Split(
                '/',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);
    }

    private static string RemoveGitExtension(string value)
    {
        if (value.EndsWith(
                ".git",
                StringComparison.OrdinalIgnoreCase))
        {
            return value[..^4];
        }

        return value;
    }
}

public class RepositoryUrlInfo
{
    public string Provider { get; set; } = string.Empty;

    public string Owner { get; set; } = string.Empty;

    public string RepoName { get; set; } = string.Empty;
}