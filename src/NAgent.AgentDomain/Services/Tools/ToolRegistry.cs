namespace NAgent.AgentDomain.Services.Tools;

/// <summary>
/// 工具注册表实现 - 内存中管理所有可用工具
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
}
