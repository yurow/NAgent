namespace NAgent.AgentApplication.Interfaces;

/// <summary>
/// 安全过滤器接口
/// </summary>
public interface IPromptFilter
{
    /// <summary>
    /// 过滤用户输入，检测并清除恶意提示词
    /// </summary>
    FilterResult Filter(string input);
}

/// <summary>
/// 沙箱结果校验器接口
/// </summary>
public interface ISandboxResultValidator
{
    /// <summary>
    /// 执行多重安全校验
    /// </summary>
    ValidationResult Validate(string output);
}

/// <summary>
/// 过滤结果
/// </summary>
public record FilterResult(bool IsSafe, string Warning, string CleanedInput);

/// <summary>
/// 校验结果
/// </summary>
public record ValidationResult(
    bool IsPassed, 
    List<string> Warnings, 
    List<CheckItem> Checks);

/// <summary>
/// 单个检查项
/// </summary>
public record CheckItem(string CheckName, bool Passed, string Message);
