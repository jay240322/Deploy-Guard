namespace Deployguard.Models;

public class Repository
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string RepoUrl { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string RepoName { get; set; } = string.Empty;
    public string DefaultBranch { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public int SizeKb { get; set; }

    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastScannedAt { get; set; }

    public AppUser? CreatedByUser { get; set; }

    public ICollection<SecretScan> SecretScans { get; set; } =
        new List<SecretScan>();

    public ICollection<PipelineFile> PipelineFiles { get; set; } =
        new List<PipelineFile>();
}