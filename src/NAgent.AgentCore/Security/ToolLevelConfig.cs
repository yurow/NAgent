namespace NAgent.AgentCore.Security;

/// <summary>
/// 工具安全等级配置 - 硬编码，LLM 不可篡改
/// </summary>
public static class ToolLevelConfig
{
    /// <summary>
    /// 工具安全等级枚举
    /// </summary>
    public enum SecurityLevel
    {
        Low,    // 低危：本地直接执行
        Medium, // 中危：需要额外验证
        High    // 高危：必须在沙箱中执行
    }

    /// <summary>
    /// 工具安全等级映射表（硬编码）
    /// </summary>
    private static readonly Dictionary<string, SecurityLevel> ToolSecurityLevels = new(StringComparer.OrdinalIgnoreCase)
    {
        // 低危工具 - 本地执行
        { "search_knowledge", SecurityLevel.Low },
        { "get_weather", SecurityLevel.Low },
        { "calculate", SecurityLevel.Low },
        { "format_date", SecurityLevel.Low },
        
        // 中危工具 - 需要验证
        { "send_email", SecurityLevel.Medium },
        { "read_file", SecurityLevel.Medium },
        
        // 高危工具 - 沙箱执行
        { "execute_code", SecurityLevel.High },
        { "run_script", SecurityLevel.High },
        { "database_write", SecurityLevel.High },
        { "system_command", SecurityLevel.High },
        { "network_request", SecurityLevel.High }
    };

    /// <summary>
    /// 获取工具的安全等级
    /// </summary>
    public static SecurityLevel GetSecurityLevel(string toolName)
    {
        if (ToolSecurityLevels.TryGetValue(toolName, out var level))
        {
            return level;
        }

        // 默认视为高危，确保安全
        return SecurityLevel.High;
    }

    /// <summary>
    /// 判断工具是否需要在沙箱中执行
    /// </summary>
    public static bool RequiresSandbox(string toolName)
    {
        var level = GetSecurityLevel(toolName);
        return level == SecurityLevel.High;
    }

    /// <summary>
    /// 注册新的工具安全等级（仅在启动时使用）
    /// </summary>
    public static void RegisterTool(string toolName, SecurityLevel level)
    {
        ToolSecurityLevels[toolName] = level;
    }
}
