namespace Deployguard.Models;

public class AdminReportViewModel
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    // Platform KPI Totals
    public int TotalUsers { get; set; }
    public int TotalRepositories { get; set; }
    public int TotalScans { get; set; }
    public int TotalSecretsFound { get; set; }
    public int TotalFilesScanned { get; set; }

    // Scan Usage & Duration
    public double TotalScanDurationSeconds { get; set; }
    public double AverageScanDurationSeconds { get; set; }
    public int CompletedScansCount { get; set; }
    public int FailedScansCount { get; set; }
    public int RunningScansCount { get; set; }

    // Most Risky Repositories
    public List<AdminRepositoryRiskViewModel> RiskyRepositories { get; set; } = new();

    // Top Vulnerabilities / Secret Types
    public List<AdminSecretTypeSummaryViewModel> TopSecretTypes { get; set; } = new();

    // Severity Breakdown
    public int CriticalSecretsCount { get; set; }
    public int HighSecretsCount { get; set; }
    public int MediumSecretsCount { get; set; }
    public int LowSecretsCount { get; set; }
}

public class AdminRepositoryRiskViewModel
{
    public int RepositoryId { get; set; }
    public string RepositoryName { get; set; } = string.Empty;
    public string RepoUrl { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string CreatedByUserName { get; set; } = string.Empty;
    public int TotalSecrets { get; set; }
    public int CriticalCount { get; set; }
    public int HighCount { get; set; }
    public int MediumCount { get; set; }
    public int LowCount { get; set; }
    public int RiskScore { get; set; }
    public string RiskLevel { get; set; } = "Safe";
    public DateTime? LastScannedAt { get; set; }
    public int TotalScans { get; set; }
}

public class AdminSecretTypeSummaryViewModel
{
    public string RuleName { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public int Count { get; set; }
}
