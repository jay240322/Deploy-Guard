namespace Deployguard.Services.Providers;

public interface IGitProvider
{
    string ProviderName { get; }

    Task<RepositoryMetadata?> GetMetadataAsync(
        string owner,
        string repoName);
}

public class RepositoryMetadata
{
    public string DefaultBranch { get; set; } = string.Empty;

    public string Language { get; set; } = string.Empty;

    public int SizeKb { get; set; }

    public DateTime? LastCommitAt { get; set; }
}