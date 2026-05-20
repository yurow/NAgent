using NAgent.AgentApplication.Interfaces;

namespace NAgent.AgentInfrastructure.Sandbox;

/// <summary>
/// CubeSandbox 执行器实现
/// </summary>
public class CubeSandboxExecutorImpl : ISandboxExecutor
{
    private readonly string _sandboxEndpoint;
    private readonly HttpClient _httpClient;

    public CubeSandboxExecutorImpl(string sandboxEndpoint)
    {
        _sandboxEndpoint = sandboxEndpoint ?? throw new ArgumentNullException(nameof(sandboxEndpoint));
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(sandboxEndpoint),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public async Task<SandboxExecutionResult> ExecuteAsync(
        string code,
        string language = "python",
        Dictionary<string, string>? environmentVariables = null,
        int timeoutSeconds = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: 调用 CubeSandbox API
            await Task.Delay(100, cancellationToken);

            return new SandboxExecutionResult(
                true,
                "沙箱执行结果示例",
                string.Empty,
                150
            );
        }
        catch (Exception ex)
        {
            return new SandboxExecutionResult(false, string.Empty, ex.Message, 0);
        }
    }

    public async Task<bool> UploadFileAsync(
        string fileName, 
        byte[] content, 
        CancellationToken cancellationToken = default)
    {
        // TODO: 实现文件上传
        await Task.Delay(10, cancellationToken);
        return true;
    }

    public async Task<byte[]?> DownloadFileAsync(
        string fileName, 
        CancellationToken cancellationToken = default)
    {
        // TODO: 实现文件下载
        await Task.Delay(10, cancellationToken);
        return null;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
