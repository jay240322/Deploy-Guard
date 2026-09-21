namespace Deployguard.Models;

public class SecretScan
{
    public int Id { get; set; }

    public int RepositoryId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int FilesScanned { get; set; }
    public int SecretsFound { get; set; }
    public string Status { get; set; } = "Running";
    public int TriggeredByUserId { get; set; }

    public Repository? Repository { get; set; }
    public AppUser? TriggeredByUser { get; set; }

    public ICollection<SecretFinding> Findings { get; set; } = new List<SecretFinding>();
}