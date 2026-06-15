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
}
