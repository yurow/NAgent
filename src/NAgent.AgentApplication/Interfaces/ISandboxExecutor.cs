namespace NAgent.AgentApplication.Interfaces;

/// <summary>
/// 沙箱执行接口 - 抽象不同沙箱实现
/// </summary>
public interface ISandboxExecutor
{
    /// <summary>
    /// 在沙箱中执行代码
    /// </summary>
    Task<SandboxExecutionResult> ExecuteAsync(
        string code,
        string language = "python",
        Dictionary<string, string>? environmentVariables = null,
        int timeoutSeconds = 30,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 上传文件到沙箱
    /// </summary>
    Task<bool> UploadFileAsync(
        string fileName, 
        byte[] content, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 从沙箱下载文件
    /// </summary>
    Task<byte[]?> DownloadFileAsync(
        string fileName, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 沙箱执行结果
/// </summary>
public record SandboxExecutionResult(
    bool IsSuccess,
    string Output,
    string Error,
    long ExecutionTimeMs);
