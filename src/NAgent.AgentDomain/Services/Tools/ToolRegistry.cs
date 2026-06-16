using NAgent.AgentDomain.Entities;
using NAgent.AgentDomain.Repositories;

namespace NAgent.AgentDomain.Services.Tools;

/// <summary>
/// 工具注册表实现 - 内存中管理所有可用工具
/// 支持内置工具和 YAML 配置工具
/// </summary>
public class ToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, IToolExecutor> _tools = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public void Register(IToolExecutor tool)
    {
        if (tool == null)
            throw new ArgumentNullException(nameof(tool));

        lock (_lock)
        {
            _tools[tool.ToolName] = tool;
        }
    }

    public IToolExecutor? GetTool(string name)
    {
        lock (_lock)
        {
            _tools.TryGetValue(name, out var tool);
            return tool;
        }
    }

    public IReadOnlyList<IToolExecutor> GetAllTools()
    {
        lock (_lock)
        {
            return _tools.Values.ToList();
        }
    }

    public bool HasTool(string name)
    {
        lock (_lock)
        {
            return _tools.ContainsKey(name);
        }
    }

    /// <summary>
    /// 从 YAML ToolDefinition 加载工具
    /// </summary>
    public void RegisterFromDefinition(ToolDefinition definition, IWorkspaceManager workspaceManager)
    {
        var executor = new YamlToolExecutor(definition, workspaceManager);
        Register(executor);
    }

    /// <summary>
    /// 批量从 YAML ToolDefinition 加载工具
    /// </summary>
    public void RegisterFromDefinitions(IEnumerable<ToolDefinition> definitions, IWorkspaceManager workspaceManager)
    {
        foreach (var definition in definitions)
        {
            if (definition.IsEnabled)
            {
                RegisterFromDefinition(definition, workspaceManager);
            }
        }
    }
}
