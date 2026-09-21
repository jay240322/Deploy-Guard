using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Deployguard.Services.Providers;

public class GitLabProvider : IGitProvider
{
    private readonly HttpClient _httpClient;

    public string ProviderName => "GitLab";

    public GitLabProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "DeployGuard");
    }

    public async Task<RepositoryMetadata?> GetMetadataAsync(
        string owner,
        string repoName)
    {
        var projectPath = $"{owner}/{repoName}";

        var encodedProjectPath =
            Uri.EscapeDataString(projectPath);

        var url =
            $"https://gitlab.com/api/v4/projects/{encodedProjectPath}";

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var project =
            await response.Content
                .ReadFromJsonAsync<GitLabProjectResponse>();

        if (project == null)
        {
            return null;
        }

        return new RepositoryMetadata
        {
            DefaultBranch = project.DefaultBranch ?? string.Empty,
            Language = string.Empty,
            SizeKb = project.Statistics?.RepositorySizeKb ?? 0,
            LastCommitAt = project.LastActivityAt
        };
    }

    private class GitLabProjectResponse
    {
        [JsonPropertyName("default_branch")]
        public string? DefaultBranch { get; set; }

        [JsonPropertyName("last_activity_at")]
        public DateTime? LastActivityAt { get; set; }

        [JsonPropertyName("statistics")]
        public GitLabStatistics? Statistics { get; set; }
    }

    private class GitLabStatistics
    {
        [JsonPropertyName("repository_size")]
        public long RepositorySize { get; set; }

        public int RepositorySizeKb =>
            RepositorySize > 0
                ? (int)(RepositorySize / 1024)
                : 0;
    }
}