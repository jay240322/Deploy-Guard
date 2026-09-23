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
            "Jenkins" => ParseJenkins(pipelineFile),
            _ => new List<PipelineStage>()
        };
    }

    // GitHub Actions Parser

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

    // Jenkins Parser

    private List<PipelineStage> ParseJenkins(
        PipelineFile pipelineFile)
    {
        var stages = new List<PipelineStage>();

        var lines = pipelineFile.RawYaml
            .Split(
                new[] { "\r\n", "\n", "\r" },
                StringSplitOptions.None);

        var order = 1;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            if (!line.StartsWith("stage("))
            {
                continue;
            }

            var stageName = ExtractJenkinsStageName(line);

            if (string.IsNullOrWhiteSpace(stageName))
            {
                continue;
            }

            var dependsOn = string.Empty;
            var condition = string.Empty;
            var isParallel = false;

            var blockEnd = FindJenkinsStageBlockEnd(
                lines,
                i);

            for (var j = i + 1;
                 j < blockEnd;
                 j++)
            {
                var innerLine = lines[j].Trim();

                if (innerLine.StartsWith("when"))
                {
                    condition = FindJenkinsCondition(
                        lines,
                        j,
                        blockEnd);
                }

                if (innerLine.StartsWith("parallel"))
                {
                    isParallel = true;
                }
            }

            /*
             * Jenkins declarative stages normally execute
             * sequentially unless parallel execution is used.
             */
            if (order > 1 &&
                string.IsNullOrWhiteSpace(dependsOn))
            {
                dependsOn = stages.LastOrDefault()?.Name
                            ?? string.Empty;
            }

            stages.Add(
                new PipelineStage
                {
                    PipelineFileId = pipelineFile.Id,
                    Name = stageName,
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

    // Jenkins Helpers

    private static string ExtractJenkinsStageName(
        string line)
    {
        // stage('Build')
        var firstSingleQuote = line.IndexOf('\'');

        if (firstSingleQuote >= 0)
        {
            var secondSingleQuote = line.IndexOf(
                '\'',
                firstSingleQuote + 1);

            if (secondSingleQuote > firstSingleQuote)
            {
                return line.Substring(
                    firstSingleQuote + 1,
                    secondSingleQuote -
                    firstSingleQuote -
                    1);
            }
        }

        // stage("Build")
        var firstDoubleQuote = line.IndexOf('"');

        if (firstDoubleQuote >= 0)
        {
            var secondDoubleQuote = line.IndexOf(
                '"',
                firstDoubleQuote + 1);

            if (secondDoubleQuote > firstDoubleQuote)
            {
                return line.Substring(
                    firstDoubleQuote + 1,
                    secondDoubleQuote -
                    firstDoubleQuote -
                    1);
            }
        }

        return string.Empty;
    }


    private static int FindJenkinsStageBlockEnd(
        string[] lines,
        int stageLineIndex)
    {
        var braceCount = 0;
        var started = false;

        for (var i = stageLineIndex;
             i < lines.Length;
             i++)
        {
            var line = lines[i];

            foreach (var character in line)
            {
                if (character == '{')
                {
                    braceCount++;
                    started = true;
                }
                else if (character == '}')
                {
                    braceCount--;
                }
            }

            if (started && braceCount <= 0)
            {
                return i;
            }
        }

        return lines.Length;
    }


    private static string FindJenkinsCondition(
        string[] lines,
        int whenLineIndex,
        int blockEnd)
    {
        for (var i = whenLineIndex + 1;
             i < blockEnd;
             i++)
        {
            var line = lines[i].Trim();

            if (line.StartsWith("branch"))
            {
                return line;
            }

            if (line.StartsWith("environment"))
            {
                return line;
            }

            if (line.StartsWith("expression"))
            {
                return line;
            }

            if (line.StartsWith("anyOf"))
            {
                return "anyOf";
            }

            if (line.StartsWith("allOf"))
            {
                return "allOf";
            }

            if (line.StartsWith("not"))
            {
                return "not";
            }
        }

        return "when";
    }

    // YAML Helper

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