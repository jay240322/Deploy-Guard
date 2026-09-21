using Microsoft.EntityFrameworkCore;
using Deployguard.Models;

namespace Deployguard.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<Repository> Repositories => Set<Repository>();
    public DbSet<SecretRule> SecretRules => Set<SecretRule>();
    public DbSet<SecretScan> SecretScans => Set<SecretScan>();
    public DbSet<SecretFinding> SecretFindings => Set<SecretFinding>();
    public DbSet<PipelineFile> PipelineFiles => Set<PipelineFile>();
    public DbSet<PipelineStage> PipelineStages => Set<PipelineStage>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
}