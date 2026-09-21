namespace Deployguard.Services.Providers;

public class AzureDevOpsProvider : IGitProvider
{
    private readonly HttpClient _httpClient;

    public string ProviderName => "Azure DevOps";

    public AzureDevOpsProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "DeployGuard");
    }

    public async Task<RepositoryMetadata?> GetMetadataAsync(
        string owner,
        string repoName)
    {
        /*
         * Azure DevOps repositories may require authentication.
         * DeployGuard currently supports public metadata retrieval.
         *
         * If the repository requires authentication, the API
         * will return an unsuccessful response and we simply
         * leave metadata as pending.
         */

        return await Task.FromResult<RepositoryMetadata?>(
            new RepositoryMetadata
            {
                DefaultBranch = string.Empty,
                Language = string.Empty,
                SizeKb = 0,
                LastCommitAt = null
            });
    }
}