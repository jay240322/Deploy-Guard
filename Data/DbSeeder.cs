using Deployguard.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Deployguard.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        IConfiguration configuration)
    {
        if (await context.AppUsers.AnyAsync(u => u.Role == "Admin"))
        {
            return;
        }

        var adminPassword = configuration["DeployGuard:AdminPassword"]
            ?? configuration["ADMIN_PASSWORD"]
            ?? "Admin@12345";

        var admin = new AppUser
        {
            UserName = "admin",
            Email = "admin@deployguard.local",
            FullName = "DeployGuard Administrator",
            Role = "Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var passwordHasher = new PasswordHasher<AppUser>();

        admin.PasswordHash = passwordHasher.HashPassword(
            admin,
            adminPassword);

        context.AppUsers.Add(admin);

        await context.SaveChangesAsync();
    }
}