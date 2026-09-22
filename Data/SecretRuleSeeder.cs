using Deployguard.Models;
using Deployguard.Services.Security;
using Microsoft.EntityFrameworkCore;

namespace Deployguard.Data;

public static class SecretRuleSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext context)
    {
        var definitions =
            SecretRuleDefinitions.GetDefaults();

        foreach (var definition in definitions)
        {
            var existingRule = await context.SecretRules
                .FirstOrDefaultAsync(r =>
                    r.Name == definition.Name);

            if (existingRule == null)
            {
                context.SecretRules.Add(
                    new SecretRule
                    {
                        Name = definition.Name,
                        Pattern = definition.Pattern,
                        Severity = definition.Severity,
                        Category = definition.Category,
                        Description = definition.Description,
                        IsActive = true
                    });
            }
        }

        await context.SaveChangesAsync();
    }
}