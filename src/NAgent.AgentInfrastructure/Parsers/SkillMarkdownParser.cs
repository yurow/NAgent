using System.Text.RegularExpressions;
using NAgent.AgentDomain.Entities;
using NAgent.AgentDomain.Services;

namespace NAgent.AgentInfrastructure.Parsers;

/// <summary>
/// Skill Markdown 文档解析器实现
/// </summary>
public class SkillMarkdownParser : ISkillParser
{
    /// <summary>
    /// 从 Markdown 内容解析 Skill
    /// </summary>
    public Skill Parse(string markdownContent, string filePath)
    {
        if (!Validate(markdownContent, out var errorMessage))
            throw new ArgumentException(errorMessage, nameof(markdownContent));

        // 解析 YAML Front Matter
        var (metadata, content) = ParseYamlFrontMatter(markdownContent);

        var name = metadata.GetValueOrDefault("name", Path.GetFileNameWithoutExtension(filePath));
        var description = metadata.GetValueOrDefault("description", "");
        var category = metadata.GetValueOrDefault("category", "general");
        var version = metadata.GetValueOrDefault("version", "1.0.0");
        var author = metadata.GetValueOrDefault("author", "system");

        var skill = new Skill(name, description, category, markdownContent, filePath);
        skill.UpdateMetadata(version, author);

        // 解析工具引用
        var toolNames = ParseToolReferences(content);
        foreach (var toolName in toolNames)
        {
            skill.AddToolName(toolName);
        }

        // 解析示例
        var examples = ParseExamples(content);
        foreach (var example in examples)
        {
            skill.AddExample(example);
        }

        return skill;
    }

    /// <summary>
    /// 从文件解析 Skill
    /// </summary>
    public async Task<Skill> ParseFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var content = await File.ReadAllTextAsync(filePath, cancellationToken);
        return Parse(content, filePath);
    }

    /// <summary>
    /// 验证 Markdown 内容是否符合 Skill 规范
    /// </summary>
    public bool Validate(string markdownContent, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(markdownContent))
        {
            errorMessage = "Skill Markdown 内容不能为空";
            return false;
        }

        // 检查是否有 YAML Front Matter
        if (!markdownContent.StartsWith("---"))
        {
            errorMessage = "Skill Markdown 必须包含 YAML Front Matter（以 --- 开头）";
            return false;
        }

        // 检查是否有内容部分
        var parts = markdownContent.Split(new[] { "---" }, StringSplitOptions.None);
        if (parts.Length < 3)
        {
            errorMessage = "Skill Markdown 格式不正确，缺少内容部分";
            return false;
        }

        return true;
    }

    /// <summary>
    /// 解析 YAML Front Matter
    /// </summary>
    private (Dictionary<string, string> metadata, string content) ParseYamlFrontMatter(string markdown)
    {
        var metadata = new Dictionary<string, string>();
        var content = markdown;

        if (markdown.StartsWith("---"))
        {
            var endIndex = markdown.IndexOf("---", 3);
            if (endIndex > 0)
            {
                var yamlPart = markdown[3..endIndex].Trim();
                content = markdown[(endIndex + 3)..].Trim();

                // 简单解析 YAML key: value 格式
                foreach (var line in yamlPart.Split('\n'))
                {
                    var trimmedLine = line.Trim();
                    if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
                        continue;

                    var colonIndex = trimmedLine.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        var key = trimmedLine[..colonIndex].Trim();
                        var value = trimmedLine[(colonIndex + 1)..].Trim();
                        // 去除引号
                        if ((value.StartsWith('"') && value.EndsWith('"')) ||
                            (value.StartsWith("'") && value.EndsWith("'")))
                        {
                            value = value[1..^1];
                        }
                        metadata[key] = value;
                    }
                }
            }
        }

        return (metadata, content);
    }

    /// <summary>
    /// 解析工具引用
    /// </summary>
    private List<string> ParseToolReferences(string content)
    {
        var tools = new List<string>();

        // 匹配 ## Tools 或 ### 工具 部分
        var toolSectionRegex = new Regex(@"#{2,3}\s*(Tools|工具).*?\n(.*?)(?=\n#{1,3}\s|$)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var toolSectionMatch = toolSectionRegex.Match(content);

        if (toolSectionMatch.Success)
        {
            var toolSection = toolSectionMatch.Groups[2].Value;
            // 匹配 - tool_name 或 * tool_name
            var toolRegex = new Regex(@"^\s*[-*]\s*(\w+)", RegexOptions.Multiline);
            foreach (Match match in toolRegex.Matches(toolSection))
            {
                tools.Add(match.Groups[1].Value);
            }
        }

        return tools;
    }

    /// <summary>
    /// 解析示例
    /// </summary>
    private List<SkillExample> ParseExamples(string content)
    {
        var examples = new List<SkillExample>();

        // 匹配 ## Examples 或 ## 示例 部分
        var exampleSectionRegex = new Regex(@"#{2,3}\s*(Examples|示例).*?\n(.*?)(?=\n#{1,3}\s|$)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var exampleSectionMatch = exampleSectionRegex.Match(content);

        if (exampleSectionMatch.Success)
        {
            var exampleSection = exampleSectionMatch.Groups[2].Value;

            // 匹配 #### Example N 或 #### 示例 N
            var exampleRegex = new Regex(@"#{4}\s*(.+?)\n.*?Input:\s*(.*?)\n.*?Output:\s*(.*?)(?=\n#{4}|$)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            foreach (Match match in exampleRegex.Matches(exampleSection))
            {
                examples.Add(new SkillExample
                {
                    Title = match.Groups[1].Value.Trim(),
                    Input = match.Groups[2].Value.Trim(),
                    Output = match.Groups[3].Value.Trim()
                });
            }
        }

        return examples;
    }
}
