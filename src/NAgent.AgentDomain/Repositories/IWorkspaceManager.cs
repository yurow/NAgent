namespace NAgent.AgentDomain.Repositories;

/// <summary>
/// 工作空间管理器接口 - 定义项目文件系统操作契约
/// </summary>
public interface IWorkspaceManager
{
    /// <summary>
    /// 获取用户工作空间路径
    /// </summary>
    string GetUserWorkspacePath(Guid userId);

    /// <summary>
    /// 获取项目工作空间路径
    /// </summary>
    string GetProjectWorkspacePath(Guid userId, Guid projectId);

    /// <summary>
    /// 创建项目配置文件
    /// </summary>
    void CreateProjectConfig(Guid userId, Guid projectId, string projectName, string description);

    /// <summary>
    /// 删除项目工作空间
    /// </summary>
    void DeleteProjectWorkspace(Guid userId, Guid projectId);

    /// <summary>
    /// 确保项目工作目录存在，并返回路径
    /// </summary>
    string EnsureProjectWorkspace(Guid userId, Guid projectId);

    /// <summary>
    /// 检查项目是否已初始化（init.md 是否存在）
    /// </summary>
    bool IsInitialized(Guid userId, Guid projectId);

    /// <summary>
    /// 确保 init.md 存在，不存在则创建（标记项目已初始化）
    /// </summary>
    string EnsureInitFile(Guid userId, Guid projectId, string projectName);

    /// <summary>
    /// 检查 spec.md 是否存在，不存在则创建
    /// </summary>
    string EnsureSpecFile(Guid userId, Guid projectId);

    /// <summary>
    /// 写入 spec.md 完整内容（用于初始化时生成）
    /// </summary>
    void WriteSpecFile(Guid userId, Guid projectId, string content);

    /// <summary>
    /// 将用户问题追加到 spec.md
    /// </summary>
    void AppendQuestionToSpec(Guid userId, Guid projectId, string question, string? answer = null);

    /// <summary>
    /// 读取 spec.md 内容
    /// </summary>
    string ReadSpecFile(Guid userId, Guid projectId);

    /// <summary>
    /// 获取工作目录下的文件列表（相对路径）
    /// </summary>
    List<string> GetWorkspaceFiles(Guid userId, Guid projectId);

    /// <summary>
    /// 获取项目工作空间的相对路径（相对于工作空间根目录）
    /// </summary>
    string GetProjectRelativePath(Guid userId, Guid projectId);

    /// <summary>
    /// 获取工作空间基础路径（绝对路径，仅内部使用）
    /// </summary>
    string GetWorkspaceBasePath();

    /// <summary>
    /// 获取项目记忆目录路径（临时记忆，按日期组织）
    /// </summary>
    string GetMemoryDirectory(Guid userId, Guid projectId);

    /// <summary>
    /// 记录 LLM 调用到记忆文件（以日期结尾的 JSON 文件）
    /// </summary>
    void RecordLlmCall(Guid userId, Guid projectId, string callType, string modelId, string prompt, string response, long durationMs, string? errorMessage = null);
}
