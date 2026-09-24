namespace Deployguard.Models;

public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int TotalRepositories { get; set; }
    public int TotalScans { get; set; }
    public int TotalSecretsFound { get; set; }

    public List<AppUser> RecentUsers { get; set; } = new();
    public List<Repository> RecentRepositories { get; set; } = new();
    public List<SecretScan> RecentScans { get; set; } = new();
}