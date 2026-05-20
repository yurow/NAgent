using NAgent.AgentCore.Agent;

namespace NAgent.AgentCore.Tools.LocalTools;

/// <summary>
/// 示例本地工具 - 计算器
/// </summary>
public class CalculatorTool : ITool
{
    public Task<string> ExecuteAsync(Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        if (!parameters.TryGetValue("expression", out var exprObj) || exprObj is not string expression)
        {
            return Task.FromResult("错误: 缺少表达式参数");
        }

        try
        {
            // TODO: 实现安全的表达式计算（避免使用 eval）
            var result = EvaluateExpression(expression);
            return Task.FromResult(result.ToString());
        }
        catch (Exception ex)
        {
            return Task.FromResult($"计算错误: {ex.Message}");
        }
    }

    private double EvaluateExpression(string expression)
    {
        // TODO: 实现安全的数学表达式解析器
        // 这里只是示例，实际应使用专门的表达式解析库
        return 0;
    }
}
