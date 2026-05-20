namespace NAgent.AgentCore.Security;

/// <summary>
/// 沙箱返回结果合规校验器 - 6重安全校验
/// </summary>
public class SandboxResultCheck
{
    private static readonly int MaxOutputLength = 10000; // 最大输出长度
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

    /// <summary>
    /// 执行6重安全校验
    /// </summary>
    public ValidationResult Validate(string output)
    {
        var checks = new List<CheckItem>();

        // 1. 空值检查
        checks.Add(CheckEmpty(output));
        
        // 2. 长度检查
        checks.Add(CheckLength(output));
        
        // 3. 恶意代码模式检查
        checks.Add(CheckMaliciousPatterns(output));
        
        // 4. SQL注入检查
        checks.Add(CheckSqlInjection(output));
        
        // 5. XSS攻击检查
        checks.Add(CheckXss(output));
        
        // 6. 敏感信息泄露检查
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
        // 检测密码、密钥等敏感信息模式
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

/// <summary>
/// 单个检查项
/// </summary>
public record CheckItem(string CheckName, bool Passed, string Message);

/// <summary>
/// 校验结果
/// </summary>
public record ValidationResult(bool IsPassed, List<string> Warnings, List<CheckItem> Checks);
