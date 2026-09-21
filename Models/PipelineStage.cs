namespace Deployguard.Models;

public class PipelineStage
{
    public int Id { get; set; }

    public int PipelineFileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public string DependsOn { get; set; } = string.Empty;
    public bool IsParallel { get; set; }
    public string Condition { get; set; } = string.Empty;
    public int EstimatedDurationSec { get; set; }

    public PipelineFile? PipelineFile { get; set; }
}