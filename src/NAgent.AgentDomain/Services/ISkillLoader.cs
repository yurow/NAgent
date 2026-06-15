using NAgent.AgentDomain.Entities;

namespace NAgent.AgentDomain.Services;

/// <summary>
/// Skill 加载器接口 - 从文件系统加载 Skills
/// </summary>
public interface ISkillLoader
{
    /// <summary>
    /// 从目录加载所有 Skills
    /// </summary>
    Task<IReadOnlyList<Skill>> LoadFromDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 加载单个 Skill 文件
    /// </summary>
    Task<Skill> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 监视目录变化并自动重新加载
    /// </summary>
    void WatchDirectory(string directoryPath);

    /// <summary>
    /// 停止监视
    /// </summary>
    void StopWatching();
}
