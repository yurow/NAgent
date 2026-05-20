namespace NAgent.AgentDomain.Enums;

/// <summary>
/// 工具安全等级
/// </summary>
public enum ToolSecurityLevel
{
    /// <summary>
    /// 低危 - 本地直接执行
    /// </summary>
    Low = 1,

    /// <summary>
    /// 中危 - 需要额外验证
    /// </summary>
    Medium = 2,

    /// <summary>
    /// 高危 - 必须在沙箱中执行
    /// </summary>
    High = 3
}
