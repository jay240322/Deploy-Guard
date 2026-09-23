namespace Deployguard.Models;

public class ReportViewModel
{
    // Repository Information
    public int RepositoryId { get; set; }

    public string RepositoryName { get; set; } = string.Empty;

    public string RepositoryUrl { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;


    // Security Scan Information
    public int ScanId { get; set; }

    public DateTime? ScanStartedAt { get; set; }

    public DateTime? ScanCompletedAt { get; set; }

    public string ScanStatus { get; set; } = string.Empty;


    // Security Summary
    public int FilesScanned { get; set; }

    public int SecretsFound { get; set; }


    // Findings by Severity
    public int CriticalCount { get; set; }

    public int HighCount { get; set; }

    public int MediumCount { get; set; }

    public int LowCount { get; set; }


    // Secret Findings
    public List<SecretFinding> Findings { get; set; } = new();


    // Pipeline Summary
    public int PipelineFileCount { get; set; }

    public int PipelineStageCount { get; set; }


    // Pipeline Information
    public List<PipelineFile> PipelineFiles { get; set; } = new();

    public List<PipelineStage> PipelineStages { get; set; } = new();


    // Report Information
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}