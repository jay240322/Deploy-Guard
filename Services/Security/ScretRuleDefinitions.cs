namespace Deployguard.Services.Security;

public static class SecretRuleDefinitions
{
    public static IReadOnlyList<SecretRuleDefinition> GetDefaults()
    {
        return new List<SecretRuleDefinition>
        {
            new()
            {
                Name = "AWS Access Key",
                Pattern = @"\bAKIA[0-9A-Z]{16}\b",
                Severity = "Critical",
                Category = "Cloud",
                Description = "Potential AWS access key."
            },

            new()
            {
                Name = "AWS Secret Key",
                Pattern = @"(?i)\baws(.{0,20})?(secret|access)[\s:=]+[A-Za-z0-9/+=]{30,45}\b",
                Severity = "Critical",
                Category = "Cloud",
                Description = "Potential AWS secret key."
            },

            new()
            {
                Name = "GitHub Token",
                Pattern = @"\bgh[pousr]_[A-Za-z0-9_]{36,}\b",
                Severity = "Critical",
                Category = "Source Control",
                Description = "Potential GitHub personal access token."
            },

            new()
            {
                Name = "GitLab Token",
                Pattern = @"\bglpat-[A-Za-z0-9_-]{20,}\b",
                Severity = "Critical",
                Category = "Source Control",
                Description = "Potential GitLab access token."
            },

            new()
            {
                Name = "Stripe Live Secret Key",
                Pattern = @"\bsk_live_[A-Za-z0-9]{20,}\b",
                Severity = "Critical",
                Category = "Payment",
                Description = "Potential Stripe live secret key."
            },

            new()
            {
                Name = "Stripe Test Secret Key",
                Pattern = @"\bsk_test_[A-Za-z0-9]{20,}\b",
                Severity = "High",
                Category = "Payment",
                Description = "Potential Stripe test secret key."
            },

            new()
            {
                Name = "JWT Secret",
                Pattern = @"(?i)(jwt[_-]?secret|jwt[_-]?key)[\s:=]+['""]?[A-Za-z0-9_\-./+=]{8,}['""]?",
                Severity = "High",
                Category = "Authentication",
                Description = "Potential hardcoded JWT signing secret."
            },

            new()
            {
                Name = "Database Password",
                Pattern = @"(?i)(database|db|mysql|postgres|postgresql|mongodb|sql)[_-]?(password|passwd|pwd)[\s:=]+['""]?[^\s'""]{6,}['""]?",
                Severity = "Critical",
                Category = "Database",
                Description = "Potential hardcoded database password."
            },

            new()
            {
                Name = "Private Key",
                Pattern = @"-----BEGIN (RSA |EC |DSA |OPENSSH )?PRIVATE KEY-----",
                Severity = "Critical",
                Category = "Cryptography",
                Description = "Potential private cryptographic key."
            },

            new()
            {
                Name = "Google API Key",
                Pattern = @"\bAIza[0-9A-Za-z_-]{35}\b",
                Severity = "High",
                Category = "Cloud",
                Description = "Potential Google API key."
            },

            new()
            {
                Name = "Slack Token",
                Pattern = @"\bxox[baprs]-[0-9A-Za-z-]{10,}\b",
                Severity = "High",
                Category = "Communication",
                Description = "Potential Slack token."
            },

            new()
            {
                Name = "Twilio API Key",
                Pattern = @"\bSK[0-9a-fA-F]{32}\b",
                Severity = "High",
                Category = "Communication",
                Description = "Potential Twilio API key."
            },

            new()
            {
                Name = "SendGrid API Key",
                Pattern = @"\bSG\.[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]{20,}\b",
                Severity = "High",
                Category = "Communication",
                Description = "Potential SendGrid API key."
            },

            new()
            {
                Name = "Generic API Key",
                Pattern = @"(?i)(api[_-]?key|apikey)[\s:=]+['""]?[A-Za-z0-9_\-]{16,}['""]?",
                Severity = "High",
                Category = "API",
                Description = "Potential hardcoded API key."
            },

            new()
            {
                Name = "Connection String",
                Pattern = @"(?i)(server|host|data source)=.+;(user id|uid)=.+;(password|pwd)=.+",
                Severity = "Critical",
                Category = "Database",
                Description = "Potential database connection string containing credentials."
            },

            new()
            {
                Name = "Hardcoded Password",
                Pattern = @"(?i)(password|passwd|pwd)[\s:=]+['""][^'""]{6,}['""]",
                Severity = "High",
                Category = "Credentials",
                Description = "Potential hardcoded password."
            }
        };
    }
}

public class SecretRuleDefinition
{
    public string Name { get; set; } = string.Empty;

    public string Pattern { get; set; } = string.Empty;

    public string Severity { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}