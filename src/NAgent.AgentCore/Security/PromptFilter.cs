namespace NAgent.AgentCore.Security;

/// <summary>
/// 提示词注入过滤器 - 清洗恶意提示词
/// </summary>
public class PromptFilter
{
    private static readonly HashSet<string> MaliciousPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        "ignore previous instructions",
        "绕过之前的指令",
        "system prompt",
        "系统提示",
        "admin mode",
        "管理员模式",
        "debug mode",
        "调试模式"
    };

    /// <summary>
    /// 过滤用户输入，检测并清除恶意提示词
    /// </summary>
    public FilterResult Filter(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new FilterResult(false, "输入不能为空", string.Empty);
        }

        var lowerInput = input.ToLowerInvariant();
        
        foreach (var pattern in MaliciousPatterns)
        {
            if (lowerInput.Contains(pattern))
            {
                return new FilterResult(
                    false, 
                    $"检测到潜在的提示词注入攻击: {pattern}", 
                    Sanitize(input, pattern)
                );
            }
        }

        return new FilterResult(true, string.Empty, input);
    }

    private string Sanitize(string input, string pattern)
    {
        // 简单的 sanitization：移除恶意模式
        return input.Replace(pattern, string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// 过滤结果
/// </summary>
public record FilterResult(bool IsSafe, string Warning, string CleanedInput);
