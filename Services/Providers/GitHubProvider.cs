using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Deployguard.Services.Providers;

public class GitHubProvider : IGitProvider
{
    private readonly HttpClient _httpClient;

    public string ProviderName => "GitHub";

    public GitHubProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "DeployGuard");
    }

    public async Task<RepositoryMetadata?> GetMetadataAsync(
        string owner,
        string repoName)
    {
        var url =
            $"https://api.github.com/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repoName)}";

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var repository =
            await response.Content.ReadFromJsonAsync<GitHubRepositoryResponse>();

        if (repository == null)
        {
            return null;
        }

        return new RepositoryMetadata
        {
            DefaultBranch = repository.DefaultBranch ?? string.Empty,
            Language = repository.Language ?? string.Empty,
            SizeKb = repository.Size,
            LastCommitAt = repository.PushedAt
        };
    }

    private class GitHubRepositoryResponse
    {
        [JsonPropertyName("default_branch")]
        public string? DefaultBranch { get; set; }

        [JsonPropertyName("language")]
        public string? Language { get; set; }

        [JsonPropertyName("size")]
        public int Size { get; set; }

        [JsonPropertyName("pushed_at")]
        public DateTime? PushedAt { get; set; }
    }
}