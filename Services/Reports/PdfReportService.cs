using Deployguard.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Deployguard.Services.Reports;

public class PdfReportService
{
    public byte[] Generate(ReportViewModel report)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(35);

                page.DefaultTextStyle(x =>
                    x.FontSize(10));

                page.Header()
                    .Column(header =>
                    {
                        header.Spacing(5);

                        header.Item()
                            .Text("DeployGuard")
                            .FontSize(24)
                            .Bold();

                        header.Item()
                            .Text("Security & Pipeline Report")
                            .FontSize(16)
                            .SemiBold();

                        header.Item()
                            .Text(
                                $"Generated: {report.GeneratedAt.ToLocalTime():dd MMM yyyy, hh:mm tt}")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken1);
                    });

                page.Content()
                    .PaddingTop(20)
                    .Column(column =>
                    {
                        column.Spacing(15);

                        // Repository Information
                        column.Item()
                            .Text("Repository Information")
                            .FontSize(15)
                            .Bold();

                        column.Item()
                            .Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(130);
                                    columns.RelativeColumn();
                                });

                                AddRow(
                                    table,
                                    "Repository",
                                    report.RepositoryName);

                                AddRow(
                                    table,
                                    "Provider",
                                    report.Provider);

                                AddRow(
                                    table,
                                    "Repository URL",
                                    report.RepositoryUrl);
                            });

                        // Security Summary
                        column.Item()
                            .Text("Security Scan Summary")
                            .FontSize(15)
                            .Bold();

                        column.Item()
                            .Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                AddSummaryCell(
                                    table,
                                    "Files Scanned",
                                    report.FilesScanned.ToString());

                                AddSummaryCell(
                                    table,
                                    "Secrets Found",
                                    report.SecretsFound.ToString());

                                AddSummaryCell(
                                    table,
                                    "Status",
                                    report.ScanStatus);

                                AddSummaryCell(
                                    table,
                                    "Critical",
                                    report.CriticalCount.ToString());

                                AddSummaryCell(
                                    table,
                                    "High",
                                    report.HighCount.ToString());

                                AddSummaryCell(
                                    table,
                                    "Medium",
                                    report.MediumCount.ToString());

                                AddSummaryCell(
                                    table,
                                    "Low",
                                    report.LowCount.ToString());

                                AddSummaryCell(
                                    table,
                                    "Scan ID",
                                    report.ScanId == 0
                                        ? "-"
                                        : report.ScanId.ToString());
                            });

                        // Secret Findings
                        column.Item()
                            .Text("Secret Findings")
                            .FontSize(15)
                            .Bold();

                        if (!report.Findings.Any())
                        {
                            column.Item()
                                .Text(
                                    "No secret findings were detected.")
                                .FontSize(10);
                        }
                        else
                        {
                            column.Item()
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(1.5f);
                                        columns.RelativeColumn(2f);
                                        columns.ConstantColumn(40);
                                        columns.ConstantColumn(65);
                                        columns.RelativeColumn(1.5f);
                                    });

                                    AddTableHeader(table, "Rule");
                                    AddTableHeader(table, "File");
                                    AddTableHeader(table, "Line");
                                    AddTableHeader(table, "Severity");
                                    AddTableHeader(table, "Masked Value");

                                    foreach (var finding in report.Findings)
                                    {
                                        table.Cell()
                                            .BorderBottom(1)
                                            .BorderColor(Colors.Grey.Lighten2)
                                            .Padding(5)
                                            .Text(
                                                finding.SecretRule?.Name
                                                ?? "Unknown");

                                        table.Cell()
                                            .BorderBottom(1)
                                            .BorderColor(Colors.Grey.Lighten2)
                                            .Padding(5)
                                            .Text(finding.FilePath);

                                        table.Cell()
                                            .BorderBottom(1)
                                            .BorderColor(Colors.Grey.Lighten2)
                                            .Padding(5)
                                            .Text(
                                                finding.LineNumber
                                                    .ToString());

                                        table.Cell()
                                            .BorderBottom(1)
                                            .BorderColor(Colors.Grey.Lighten2)
                                            .Padding(5)
                                            .Text(finding.Severity);

                                        table.Cell()
                                            .BorderBottom(1)
                                            .BorderColor(Colors.Grey.Lighten2)
                                            .Padding(5)
                                            .Text(finding.MaskedValue);
                                    }
                                });
                        }

                        // Pipeline Summary
                        column.Item()
                            .Text("CI/CD Pipeline Summary")
                            .FontSize(15)
                            .Bold();

                        column.Item()
                            .Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                AddSummaryCell(
                                    table,
                                    "Pipeline Files",
                                    report.PipelineFileCount.ToString());

                                AddSummaryCell(
                                    table,
                                    "Pipeline Stages",
                                    report.PipelineStageCount.ToString());
                            });

                        // Pipeline Files
                        if (report.PipelineFiles.Any())
                        {
                            column.Item()
                                .Text("Detected Pipeline Files")
                                .FontSize(13)
                                .Bold();

                            column.Item()
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn();
                                        columns.ConstantColumn(60);
                                    });

                                    AddTableHeader(
                                        table,
                                        "File");

                                    AddTableHeader(
                                        table,
                                        "Provider");

                                    AddTableHeader(
                                        table,
                                        "Stages");

                                    foreach (var pipeline in report.PipelineFiles)
                                    {
                                        table.Cell()
                                            .BorderBottom(1)
                                            .BorderColor(Colors.Grey.Lighten2)
                                            .Padding(5)
                                            .Text(pipeline.FilePath);

                                        table.Cell()
                                            .BorderBottom(1)
                                            .BorderColor(Colors.Grey.Lighten2)
                                            .Padding(5)
                                            .Text(pipeline.Provider);

                                        table.Cell()
                                            .BorderBottom(1)
                                            .BorderColor(Colors.Grey.Lighten2)
                                            .Padding(5)
                                            .Text(
                                                pipeline.Stages.Count
                                                    .ToString());
                                    }
                                });
                        }

                        // Pipeline Stages
                        if (report.PipelineStages.Any())
                        {
                            column.Item()
                                .Text("Pipeline Stages")
                                .FontSize(13)
                                .Bold();

                            column.Item()
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(40);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(1.5f);
                                        columns.ConstantColumn(55);
                                        columns.RelativeColumn(1.5f);
                                    });

                                    AddTableHeader(table, "Order");
                                    AddTableHeader(table, "Stage");
                                    AddTableHeader(table, "Depends On");
                                    AddTableHeader(table, "Parallel");
                                    AddTableHeader(table, "Condition");

                                    foreach (var stage in report.PipelineStages)
                                    {
                                        table.Cell()
                                            .BorderBottom(1)
                                            .BorderColor(Colors.Grey.Lighten2)
                                            .Padding(5)
                                            .Text(stage.Order.ToString());

                                        table.Cell()
                                            .BorderBottom(1)
                                            .BorderColor(Colors.Grey.Lighten2)
                                            .Padding(5)
                                            .Text(stage.Name);

                                        table.Cell()
                                            .BorderBottom(1)
                                            .BorderColor(Colors.Grey.Lighten2)
                                            .Padding(5)
                                            .Text(
                                                string.IsNullOrWhiteSpace(stage.DependsOn)
                                                    ? "-"
                                                    : stage.DependsOn);

                                        table.Cell()
                                            .BorderBottom(1)
                                            .BorderColor(Colors.Grey.Lighten2)
                                            .Padding(5)
                                            .Text(
                                                stage.IsParallel
                                                    ? "Yes"
                                                    : "No");

                                        table.Cell()
                                            .BorderBottom(1)
                                            .BorderColor(Colors.Grey.Lighten2)
                                            .Padding(5)
                                            .Text(
                                                string.IsNullOrWhiteSpace(stage.Condition)
                                                    ? "-"
                                                    : stage.Condition);
                                    }
                                });
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("DeployGuard • Security & Pipeline Report • ");

                        text.CurrentPageNumber();

                        text.Span(" / ");

                        text.TotalPages();
                    });
            });
        });

        return document.GeneratePdf();
    }

    private static void AddRow(
        TableDescriptor table,
        string label,
        string value)
    {
        table.Cell()
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(5)
            .Text(label)
            .Bold();

        table.Cell()
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(5)
            .Text(value);
    }

    private static void AddSummaryCell(
        TableDescriptor table,
        string label,
        string value)
    {
        table.Cell()
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(8)
            .Column(column =>
            {
                column.Item()
                    .Text(label)
                    .FontSize(8)
                    .FontColor(Colors.Grey.Darken1);

                column.Item()
                    .Text(value)
                    .FontSize(13)
                    .Bold();
            });
    }

    private static void AddTableHeader(
        TableDescriptor table,
        string text)
    {
        table.Cell()
            .Background(Colors.Grey.Lighten3)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Medium)
            .Padding(5)
            .Text(text)
            .Bold();
    }
}