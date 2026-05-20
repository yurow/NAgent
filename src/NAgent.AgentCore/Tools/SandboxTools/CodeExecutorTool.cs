using NAgent.AgentCore.Agent;
using NAgent.AgentCore.Sandbox;

namespace NAgent.AgentCore.Tools.SandboxTools;

/// <summary>
/// 示例沙箱工具 - 代码执行器
/// </summary>
public class CodeExecutorTool : ISandboxTool
{
    private readonly CubeSandboxClient _sandboxClient;

    public CodeExecutorTool(CubeSandboxClient sandboxClient)
    {
        _sandboxClient = sandboxClient ?? throw new ArgumentNullException(nameof(sandboxClient));
    }

    public async Task<string> ExecuteInSandboxAsync(Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        if (!parameters.TryGetValue("code", out var codeObj) || codeObj is not string code)
        {
            return "错误: 缺少代码参数";
        }

        if (!parameters.TryGetValue("language", out var langObj) || langObj is not string language)
        {
            language = "python";
        }

        try
        {
            var result = await _sandboxClient.ExecuteAsync(code, language, cancellationToken: cancellationToken);
            
            if (result.IsSuccess)
            {
                return result.Output;
            }
            else
            {
                return $"沙箱执行错误: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            return $"执行异常: {ex.Message}";
        }
    }
}
