using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Deployguard.Services.Providers;

public class BitbucketProvider : IGitProvider
{
    private readonly HttpClient _httpClient;

    public string ProviderName => "Bitbucket";

    public BitbucketProvider(HttpClient httpClient)
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
            $"https://api.bitbucket.org/2.0/repositories/" +
            $"{Uri.EscapeDataString(owner)}/" +
            $"{Uri.EscapeDataString(repoName)}";

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var repository =
            await response.Content
                .ReadFromJsonAsync<BitbucketRepositoryResponse>();

        if (repository == null)
        {
            return null;
        }

        return new RepositoryMetadata
        {
            DefaultBranch =
                repository.MainBranch?.Name ?? string.Empty,

            Language =
                repository.Language ?? string.Empty,

            SizeKb =
                repository.Size > 0
                    ? (int)(repository.Size / 1024)
                    : 0,

            LastCommitAt =
                repository.UpdatedOn
        };
    }

    private class BitbucketRepositoryResponse
    {
        [JsonPropertyName("mainbranch")]
        public BitbucketBranch? MainBranch { get; set; }

        [JsonPropertyName("language")]
        public string? Language { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("updated_on")]
        public DateTime? UpdatedOn { get; set; }
    }

    private class BitbucketBranch
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}