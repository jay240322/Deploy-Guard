namespace Deployguard.Models;

public class AdminUsersViewModel
{
    public List<AdminUserItemViewModel> Users { get; set; } = new();
    public int CurrentUserId { get; set; }
}

public class AdminUserItemViewModel
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int RepositoriesCount { get; set; }
    public int ScansCount { get; set; }
    public int SecretsFoundCount { get; set; }
}
