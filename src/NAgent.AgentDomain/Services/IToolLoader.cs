using NAgent.AgentDomain.Entities;

namespace NAgent.AgentDomain.Services;

/// <summary>
/// Tool 加载器接口 - 从文件系统加载 Tools
/// </summary>
public interface IToolLoader
{
    /// <summary>
    /// 从目录加载所有 Tools
    /// </summary>
    Task<IReadOnlyList<ToolDefinition>> LoadFromDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 加载单个 Tool 文件
    /// </summary>
    Task<ToolDefinition> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 监视目录变化并自动重新加载
    /// </summary>
    void WatchDirectory(string directoryPath);

    /// <summary>
    /// 停止监视
    /// </summary>
    void StopWatching();
}
