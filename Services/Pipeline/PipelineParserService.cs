using System.Text.Json;
using Deployguard.Models;
using YamlDotNet.RepresentationModel;

namespace Deployguard.Services.Pipeline;

public class PipelineParserService
{
    public List<PipelineStage> Parse(
        PipelineFile pipelineFile)
    {
        return pipelineFile.Provider switch
        {
            "GitHub Actions" => ParseGitHubActions(pipelineFile),
            "GitLab CI" => ParseGitLabCi(pipelineFile),
            "Azure Pipelines" => ParseAzurePipelines(pipelineFile),
            "CircleCI" => ParseCircleCi(pipelineFile),
            "Bitbucket Pipelines" => ParseBitbucketPipelines(pipelineFile),
            _ => new List<PipelineStage>()
        };
    }

    private List<PipelineStage> ParseGitHubActions(
        PipelineFile pipelineFile)
    {
        var stages = new List<PipelineStage>();

        var yaml = new YamlStream();

        using var reader = new StringReader(
            pipelineFile.RawYaml);

        yaml.Load(reader);

        if (yaml.Documents.Count == 0)
        {
            return stages;
        }

        var root = (YamlMappingNode)
            yaml.Documents[0].RootNode;

        if (!root.Children.TryGetValue(
                new YamlScalarNode("jobs"),
                out var jobsNode))
        {
            return stages;
        }

        if (jobsNode is not YamlMappingNode jobs)
        {
            return stages;
        }

        var order = 1;

        foreach (var job in jobs.Children)
        {
            var jobName = job.Key.ToString();

            if (job.Value is not YamlMappingNode jobConfig)
            {
                continue;
            }

            var dependsOn = string.Empty;

            if (jobConfig.Children.TryGetValue(
                    new YamlScalarNode("needs"),
                    out var needsNode))
            {
                dependsOn = ConvertYamlValueToString(
                    needsNode);
            }

            var condition = string.Empty;

            if (jobConfig.Children.TryGetValue(
                    new YamlScalarNode("if"),
                    out var conditionNode))
            {
                condition = conditionNode.ToString();
            }

            var isParallel =
                !string.IsNullOrWhiteSpace(dependsOn) &&
                dependsOn.Contains(",");

            stages.Add(
                new PipelineStage
                {
                    PipelineFileId = pipelineFile.Id,
                    Name = jobName,
                    Order = order,
                    DependsOn = dependsOn,
                    IsParallel = isParallel,
                    Condition = condition,
                    EstimatedDurationSec = 0
                });

            order++;
        }

        return stages;
    }

    private List<PipelineStage> ParseGitLabCi(
        PipelineFile pipelineFile)
    {
        // GitLab parser will be added next.
        return new List<PipelineStage>();
    }

    private List<PipelineStage> ParseAzurePipelines(
        PipelineFile pipelineFile)
    {
        // Azure parser will be added next.
        return new List<PipelineStage>();
    }

    private List<PipelineStage> ParseCircleCi(
        PipelineFile pipelineFile)
    {
        // CircleCI parser will be added next.
        return new List<PipelineStage>();
    }

    private List<PipelineStage> ParseBitbucketPipelines(
        PipelineFile pipelineFile)
    {
        // Bitbucket parser will be added next.
        return new List<PipelineStage>();
    }

    private static string ConvertYamlValueToString(
        YamlNode node)
    {
        if (node is YamlScalarNode scalar)
        {
            return scalar.Value ?? string.Empty;
        }

        if (node is YamlSequenceNode sequence)
        {
            return string.Join(
                ", ",
                sequence.Children.Select(
                    child => child.ToString()));
        }

        return node.ToString();
    }
}