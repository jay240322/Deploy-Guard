using Deployguard.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Deployguard.Services.Reports;

public class AdminPdfReportService
{
    public byte[] Generate(AdminReportViewModel report)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(35);
                page.DefaultTextStyle(x => x.FontSize(9));

                // Header
                page.Header()
                    .Column(header =>
                    {
                        header.Spacing(4);

                        header.Item()
                            .Row(row =>
                            {
                                row.RelativeItem()
                                    .Column(c =>
                                    {
                                        c.Item().Text("DeployGuard").FontSize(22).Bold().FontColor(Colors.Blue.Darken3);
                                        c.Item().Text("Executive Security & CI/CD Platform Health Report").FontSize(14).SemiBold().FontColor(Colors.Grey.Darken3);
                                    });

                                row.ConstantItem(140)
                                    .AlignRight()
                                    .Column(c =>
                                    {
                                        c.Item().Text($"Generated (UTC):").FontSize(8).FontColor(Colors.Grey.Medium);
                                        c.Item().Text($"{report.GeneratedAt:dd MMM yyyy HH:mm}").FontSize(9).Bold();
                                        c.Item().Text("Confidential • Admin Only").FontSize(8).FontColor(Colors.Red.Darken2).SemiBold();
                                    });
                            });

                        header.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                // Content
                page.Content()
                    .PaddingTop(15)
                    .Column(column =>
                    {
                        column.Spacing(14);

                        // 1. Executive Summary KPIs
                        column.Item().Text("1. Platform Overview & Key Metrics").FontSize(13).Bold().FontColor(Colors.Blue.Darken2);

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            AddMetricCell(table, "Total Users", report.TotalUsers.ToString(), Colors.Blue.Darken2);
                            AddMetricCell(table, "Repositories", report.TotalRepositories.ToString(), Colors.Cyan.Darken2);
                            AddMetricCell(table, "Total Scans", report.TotalScans.ToString(), Colors.Green.Darken2);
                            AddMetricCell(table, "Files Scanned", report.TotalFilesScanned.ToString("N0"), Colors.Grey.Darken3);
                            AddMetricCell(table, "Secrets Found", report.TotalSecretsFound.ToString(), report.TotalSecretsFound > 0 ? Colors.Red.Darken2 : Colors.Green.Darken2);
                        });

                        // 2. Scan Usage & Performance Analytics
                        column.Item().Text("2. Scan Usage & Execution Analytics").FontSize(13).Bold().FontColor(Colors.Blue.Darken2);

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            AddMetricCell(table, "Total Scan Runtime", FormatDuration(report.TotalScanDurationSeconds), Colors.Grey.Darken3);
                            AddMetricCell(table, "Average Scan Time", $"{report.AverageScanDurationSeconds:F1} sec", Colors.Grey.Darken3);
                            AddMetricCell(table, "Completed Scans", report.CompletedScansCount.ToString(), Colors.Green.Darken2);
                            AddMetricCell(table, "Failed Scans", report.FailedScansCount.ToString(), report.FailedScansCount > 0 ? Colors.Red.Darken2 : Colors.Grey.Darken1);
                        });

                        // 3. Vulnerability & Severity Distribution
                        column.Item().Text("3. Threat & Severity Breakdown").FontSize(13).Bold().FontColor(Colors.Blue.Darken2);

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            AddMetricCell(table, "Critical Severity", report.CriticalSecretsCount.ToString(), Colors.Red.Darken3);
                            AddMetricCell(table, "High Severity", report.HighSecretsCount.ToString(), Colors.Orange.Darken3);
                            AddMetricCell(table, "Medium Severity", report.MediumSecretsCount.ToString(), Colors.Amber.Darken3);
                            AddMetricCell(table, "Low Severity", report.LowSecretsCount.ToString(), Colors.Blue.Darken1);
                        });

                        // 4. Most Risky Repositories
                        column.Item().Text("4. High Risk Repositories (Ranked by Vulnerability Score)").FontSize(13).Bold().FontColor(Colors.Blue.Darken2);

                        if (!report.RiskyRepositories.Any())
                        {
                            column.Item().Text("No repositories found or registered.").Italic().FontColor(Colors.Grey.Darken1);
                        }
                        else
                        {
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2.5f);
                                    columns.RelativeColumn(1.2f);
                                    columns.RelativeColumn(1.2f);
                                    columns.ConstantColumn(55);
                                    columns.ConstantColumn(65);
                                    columns.RelativeColumn(1.5f);
                                });

                                AddTableHeader(table, "Repository");
                                AddTableHeader(table, "Provider");
                                AddTableHeader(table, "Owner");
                                AddTableHeader(table, "Secrets");
                                AddTableHeader(table, "Risk Level");
                                AddTableHeader(table, "Last Scanned");

                                foreach (var repo in report.RiskyRepositories.Take(15))
                                {
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                                        .Text(repo.RepositoryName).SemiBold();

                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                                        .Text(repo.Provider);

                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                                        .Text(string.IsNullOrEmpty(repo.Owner) ? repo.CreatedByUserName : repo.Owner);

                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                                        .Text(repo.TotalSecrets.ToString()).Bold();

                                    var riskColor = repo.RiskLevel switch
                                    {
                                        "Critical Risk" => Colors.Red.Darken2,
                                        "High Risk" => Colors.Orange.Darken2,
                                        "Moderate Risk" => Colors.Amber.Darken3,
                                        "Low Risk" => Colors.Blue.Darken1,
                                        _ => Colors.Green.Darken2
                                    };

                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                                        .Text(repo.RiskLevel).Bold().FontColor(riskColor);

                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                                        .Text(repo.LastScannedAt.HasValue
                                            ? repo.LastScannedAt.Value.ToLocalTime().ToString("dd MMM yyyy")
                                            : "Never").FontSize(8);
                                }
                            });
                        }

                        // 5. Top Secret Rule Violations
                        if (report.TopSecretTypes.Any())
                        {
                            column.Item().Text("5. Top Detected Secret Types").FontSize(13).Bold().FontColor(Colors.Blue.Darken2);

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(1.5f);
                                    columns.ConstantColumn(70);
                                });

                                AddTableHeader(table, "Secret Rule / Pattern");
                                AddTableHeader(table, "Severity");
                                AddTableHeader(table, "Occurrences");

                                foreach (var type in report.TopSecretTypes.Take(8))
                                {
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                                        .Text(type.RuleName);

                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                                        .Text(type.Severity).SemiBold();

                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                                        .Text(type.Count.ToString()).Bold();
                                }
                            });
                        }
                    });

                // Footer
                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("DeployGuard Security Intelligence • Automated Admin Audit Report • Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
            });
        });

        return document.GeneratePdf();
    }

    private static string FormatDuration(double seconds)
    {
        if (seconds < 60)
            return $"{seconds:F0} sec";
        var mins = (int)(seconds / 60);
        var remainingSec = (int)(seconds % 60);
        return $"{mins}m {remainingSec}s";
    }

    private static void AddMetricCell(TableDescriptor table, string label, string value, string colorHex)
    {
        table.Cell()
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.Grey.Lighten4)
            .Padding(7)
            .Column(c =>
            {
                c.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Darken1);
                c.Item().Text(value).FontSize(13).Bold().FontColor(colorHex);
            });
    }

    private static void AddTableHeader(TableDescriptor table, string text)
    {
        table.Cell()
            .Background(Colors.Grey.Lighten3)
            .BorderBottom(1.5f)
            .BorderColor(Colors.Grey.Darken1)
            .Padding(4)
            .Text(text)
            .Bold()
            .FontSize(8.5f);
    }
}
