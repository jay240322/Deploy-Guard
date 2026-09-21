namespace Deployguard.Models;

public class SecretFinding
{
    public int Id { get; set; }

    public int SecretScanId { get; set; }
    public int SecretRuleId { get; set; }

    public string FilePath { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public string MaskedValue { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public bool IsFalsePositive { get; set; }
    public string Remarks { get; set; } = string.Empty;

    public SecretScan? SecretScan { get; set; }
    public SecretRule? SecretRule { get; set; }
}