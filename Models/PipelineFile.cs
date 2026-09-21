namespace Deployguard.Models;

public class PipelineFile
{
    public int Id { get; set; }

    public int RepositoryId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string RawYaml { get; set; } = string.Empty;
    public string ParsedJson { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    public Repository? Repository { get; set; }

    public ICollection<PipelineStage> Stages { get; set; } = new List<PipelineStage>();
}