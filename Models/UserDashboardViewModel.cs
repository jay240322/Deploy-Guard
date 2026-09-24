namespace Deployguard.Models;

public class UserDashboardViewModel
{
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    public int RepositoryCount { get; set; }
    public int ScanCount { get; set; }
    public int SecretsFound { get; set; }

    public List<Repository> RecentRepositories { get; set; }
        = new();

    public List<SecretScan> RecentScans { get; set; }
        = new();
}