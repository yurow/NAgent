using NAgent.AgentApplication.Interfaces;

namespace NAgent.AgentInfrastructure.Security;

/// <summary>
/// 提示词过滤器实现
/// </summary>
public class PromptFilterImpl : IPromptFilter
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
        return input.Replace(pattern, string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
