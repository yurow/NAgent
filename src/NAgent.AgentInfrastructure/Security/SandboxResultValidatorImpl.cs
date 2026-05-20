using NAgent.AgentApplication.Interfaces;

namespace NAgent.AgentInfrastructure.Security;

/// <summary>
/// 沙箱结果校验器实现 - 6重安全校验
/// </summary>
public class SandboxResultValidatorImpl : ISandboxResultValidator
{
    private static readonly int MaxOutputLength = 10000;
    private static readonly HashSet<string> BlockedPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        "<script>",
        "javascript:",
        "eval(",
        "exec(",
        "__import__",
        "os.system",
        "subprocess"
    };

    public ValidationResult Validate(string output)
    {
        var checks = new List<CheckItem>();

        checks.Add(CheckEmpty(output));
        checks.Add(CheckLength(output));
        checks.Add(CheckMaliciousPatterns(output));
        checks.Add(CheckSqlInjection(output));
        checks.Add(CheckXss(output));
        checks.Add(CheckSensitiveData(output));

        var isPassed = checks.All(c => c.Passed);
        var warnings = checks.Where(c => !c.Passed).Select(c => c.Message).ToList();

        return new ValidationResult(isPassed, warnings, checks);
    }

    private CheckItem CheckEmpty(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return new CheckItem("空值检查", false, "输出为空");
        }
        return new CheckItem("空值检查", true, string.Empty);
    }

    private CheckItem CheckLength(string output)
    {
        if (output.Length > MaxOutputLength)
        {
            return new CheckItem("长度检查", false, $"输出长度 {output.Length} 超过限制 {MaxOutputLength}");
        }
        return new CheckItem("长度检查", true, string.Empty);
    }

    private CheckItem CheckMaliciousPatterns(string output)
    {
        var lowerOutput = output.ToLowerInvariant();
        foreach (var pattern in BlockedPatterns)
        {
            if (lowerOutput.Contains(pattern))
            {
                return new CheckItem("恶意代码检查", false, $"检测到恶意模式: {pattern}");
            }
        }
        return new CheckItem("恶意代码检查", true, string.Empty);
    }

    private CheckItem CheckSqlInjection(string output)
    {
        var sqlPatterns = new[] { "DROP TABLE", "DELETE FROM", "INSERT INTO", "UPDATE.*SET" };
        var upperOutput = output.ToUpperInvariant();
        
        foreach (var pattern in sqlPatterns)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(upperOutput, pattern))
            {
                return new CheckItem("SQL注入检查", false, "检测到潜在的SQL注入");
            }
        }
        return new CheckItem("SQL注入检查", true, string.Empty);
    }

    private CheckItem CheckXss(string output)
    {
        var xssPatterns = new[] { "<script", "onerror=", "onclick=", "javascript:" };
        var lowerOutput = output.ToLowerInvariant();
        
        foreach (var pattern in xssPatterns)
        {
            if (lowerOutput.Contains(pattern))
            {
                return new CheckItem("XSS检查", false, "检测到潜在的XSS攻击");
            }
        }
        return new CheckItem("XSS检查", true, string.Empty);
    }

    private CheckItem CheckSensitiveData(string output)
    {
        var sensitivePatterns = new[] { "password=", "secret_key=", "api_key=", "token=" };
        var lowerOutput = output.ToLowerInvariant();
        
        foreach (var pattern in sensitivePatterns)
        {
            if (lowerOutput.Contains(pattern))
            {
                return new CheckItem("敏感数据检查", false, "检测到敏感信息泄露");
            }
        }
        return new CheckItem("敏感数据检查", true, string.Empty);
    }
}
