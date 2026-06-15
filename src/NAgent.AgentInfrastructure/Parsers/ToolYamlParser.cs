using NAgent.AgentDomain.Entities;
using NAgent.AgentDomain.Enums;
using NAgent.AgentDomain.Services;
using YamlDotNet.RepresentationModel;

namespace NAgent.AgentInfrastructure.Parsers;

/// <summary>
/// Tool YAML 配置解析器实现
/// </summary>
public class ToolYamlParser : IToolDefinitionParser
{
    /// <summary>
    /// 从 YAML 内容解析 Tool 定义
    /// </summary>
    public ToolDefinition Parse(string yamlContent, string filePath)
    {
        if (!Validate(yamlContent, out var errorMessage))
            throw new ArgumentException(errorMessage, nameof(yamlContent));

        var yaml = new YamlStream();
        yaml.Load(new StringReader(yamlContent));

        var root = yaml.Documents[0].RootNode as YamlMappingNode;
        if (root == null)
            throw new ArgumentException("YAML 根节点必须是映射类型");

        var name = GetStringValue(root, "name") ?? Path.GetFileNameWithoutExtension(filePath);
        var description = GetStringValue(root, "description") ?? "";
        var category = GetStringValue(root, "category") ?? "general";

        var tool = new ToolDefinition(name, description, category, yamlContent, filePath);

        // 解析安全等级
        var securityLevelStr = GetStringValue(root, "security_level");
        if (!string.IsNullOrEmpty(securityLevelStr))
        {
            var level = securityLevelStr.ToLower() switch
            {
                "low" => ToolSecurityLevel.Low,
                "medium" => ToolSecurityLevel.Medium,
                "high" => ToolSecurityLevel.High,
                _ => ToolSecurityLevel.Low
            };
            tool.SetSecurityLevel(level);
        }

        // 解析参数
        if (root.Children.TryGetValue(new YamlScalarNode("parameters"), out var paramsNode) &&
            paramsNode is YamlSequenceNode paramsSequence)
        {
            foreach (var paramNode in paramsSequence)
            {
                if (paramNode is YamlMappingNode paramMapping)
                {
                    var parameter = ParseParameter(paramMapping);
                    tool.AddParameter(parameter);
                }
            }
        }

        // 解析执行配置
        if (root.Children.TryGetValue(new YamlScalarNode("execution"), out var execNode) &&
            execNode is YamlMappingNode execMapping)
        {
            var config = ParseExecutionConfig(execMapping);
            tool.SetExecutionConfig(config);
        }

        return tool;
    }

    /// <summary>
    /// 从文件解析 Tool 定义
    /// </summary>
    public async Task<ToolDefinition> ParseFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var content = await File.ReadAllTextAsync(filePath, cancellationToken);
        return Parse(content, filePath);
    }

    /// <summary>
    /// 验证 YAML 内容是否符合 Tool 规范
    /// </summary>
    public bool Validate(string yamlContent, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(yamlContent))
        {
            errorMessage = "Tool YAML 内容不能为空";
            return false;
        }

        try
        {
            var yaml = new YamlStream();
            yaml.Load(new StringReader(yamlContent));

            if (yaml.Documents.Count == 0)
            {
                errorMessage = "YAML 文档为空";
                return false;
            }

            var root = yaml.Documents[0].RootNode as YamlMappingNode;
            if (root == null)
            {
                errorMessage = "YAML 根节点必须是映射类型";
                return false;
            }

            // 检查必需字段
            if (!root.Children.ContainsKey(new YamlScalarNode("name")))
            {
                errorMessage = "Tool YAML 必须包含 name 字段";
                return false;
            }

            if (!root.Children.ContainsKey(new YamlScalarNode("description")))
            {
                errorMessage = "Tool YAML 必须包含 description 字段";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"YAML 解析错误: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// 获取字符串值
    /// </summary>
    private string? GetStringValue(YamlMappingNode mapping, string key)
    {
        if (mapping.Children.TryGetValue(new YamlScalarNode(key), out var node) && node is YamlScalarNode scalar)
        {
            return scalar.Value;
        }
        return null;
    }

    /// <summary>
    /// 解析参数定义
    /// </summary>
    private ToolParameter ParseParameter(YamlMappingNode paramMapping)
    {
        var parameter = new ToolParameter
        {
            Name = GetStringValue(paramMapping, "name") ?? "",
            Description = GetStringValue(paramMapping, "description") ?? "",
            Type = GetStringValue(paramMapping, "type") ?? "string",
            Required = GetStringValue(paramMapping, "required")?.ToLower() != "false"
        };

        // 解析默认值
        if (paramMapping.Children.TryGetValue(new YamlScalarNode("default"), out var defaultNode))
        {
            parameter.DefaultValue = defaultNode is YamlScalarNode scalar ? scalar.Value : null;
        }

        // 解析枚举值
        if (paramMapping.Children.TryGetValue(new YamlScalarNode("enum"), out var enumNode) &&
            enumNode is YamlSequenceNode enumSequence)
        {
            parameter.EnumValues = enumSequence
                .OfType<YamlScalarNode>()
                .Select(n => n.Value ?? "")
                .ToList();
        }

        return parameter;
    }

    /// <summary>
    /// 解析执行配置
    /// </summary>
    private ToolExecutionConfig ParseExecutionConfig(YamlMappingNode execMapping)
    {
        var config = new ToolExecutionConfig
        {
            ExecutionType = GetStringValue(execMapping, "type") ?? "local",
            Command = GetStringValue(execMapping, "command"),
            Endpoint = GetStringValue(execMapping, "endpoint"),
            HttpMethod = GetStringValue(execMapping, "http_method"),
            WorkingDirectory = GetStringValue(execMapping, "working_directory")
        };

        // 解析超时时间
        if (execMapping.Children.TryGetValue(new YamlScalarNode("timeout_seconds"), out var timeoutNode) &&
            timeoutNode is YamlScalarNode timeoutScalar &&
            int.TryParse(timeoutScalar.Value, out var timeout))
        {
            config.TimeoutSeconds = timeout;
        }

        // 解析环境变量
        if (execMapping.Children.TryGetValue(new YamlScalarNode("environment"), out var envNode) &&
            envNode is YamlMappingNode envMapping)
        {
            config.EnvironmentVariables = new Dictionary<string, string>();
            foreach (var kvp in envMapping.Children)
            {
                if (kvp.Key is YamlScalarNode keyNode && kvp.Value is YamlScalarNode valueNode)
                {
                    config.EnvironmentVariables[keyNode.Value ?? ""] = valueNode.Value ?? "";
                }
            }
        }

        return config;
    }
}
