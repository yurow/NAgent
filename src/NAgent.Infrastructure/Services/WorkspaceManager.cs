using NAgent.AgentDomain.Repositories;

namespace NAgent.Infrastructure.Services;

/// <summary>
/// 工作空间管理器 - 管理用户和项目的文件系统结构
/// </summary>
public class WorkspaceManager : IWorkspaceManager
{
    private readonly string _workspaceBasePath;

    public WorkspaceManager(string workspaceBasePath)
    {
        _workspaceBasePath = workspaceBasePath ?? throw new ArgumentNullException(nameof(workspaceBasePath));
        Directory.CreateDirectory(_workspaceBasePath);
    }

    /// <summary>
    /// 获取用户工作空间路径
    /// </summary>
    public string GetUserWorkspacePath(Guid userId)
    {
        var userPath = Path.Combine(_workspaceBasePath, userId.ToString());
        Directory.CreateDirectory(userPath);
        return userPath;
    }

    /// <summary>
    /// 获取项目工作空间路径
    /// </summary>
    public string GetProjectWorkspacePath(Guid userId, Guid projectId)
    {
        var userPath = GetUserWorkspacePath(userId);
        var projectPath = Path.Combine(userPath, projectId.ToString());
        Directory.CreateDirectory(projectPath);
        return projectPath;
    }

    /// <summary>
    /// 获取会话路径
    /// </summary>
    public string GetSessionPath(Guid userId, Guid projectId, Guid sessionId)
    {
        var projectPath = GetProjectWorkspacePath(userId, projectId);
        var sessionPath = Path.Combine(projectPath, "sessions", sessionId.ToString());
        Directory.CreateDirectory(sessionPath);
        return sessionPath;
    }

    /// <summary>
    /// 获取项目列表文件路径
    /// </summary>
    public string GetProjectListFilePath(Guid userId)
    {
        var userPath = GetUserWorkspacePath(userId);
        return Path.Combine(userPath, "projects.json");
    }

    /// <summary>
    /// 检查项目列表文件是否存在
    /// </summary>
    public bool HasProjectList(Guid userId)
    {
        return File.Exists(GetProjectListFilePath(userId));
    }

    /// <summary>
    /// 获取用户的所有项目目录
    /// </summary>
    public List<string> GetUserProjectDirectories(Guid userId)
    {
        var userPath = GetUserWorkspacePath(userId);
        if (!Directory.Exists(userPath))
        {
            return new List<string>();
        }

        var directories = Directory.GetDirectories(userPath)
            .Where(d => !Path.GetFileName(d).Equals("sessions", StringComparison.OrdinalIgnoreCase))
            .ToList();

        return directories;
    }

    /// <summary>
    /// 删除项目工作空间
    /// </summary>
    public void DeleteProjectWorkspace(Guid userId, Guid projectId)
    {
        var projectPath = GetProjectWorkspacePath(userId, projectId);
        if (Directory.Exists(projectPath))
        {
            Directory.Delete(projectPath, true);
        }
    }

    /// <summary>
    /// 创建项目配置文件
    /// </summary>
    public void CreateProjectConfig(Guid userId, Guid projectId, string projectName, string description)
    {
        var projectPath = GetProjectWorkspacePath(userId, projectId);
        var configPath = Path.Combine(projectPath, "project.json");

        var config = new
        {
            ProjectId = projectId,
            ProjectName = projectName,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            UserId = userId
        };

        var json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(configPath, json);
    }

    /// <summary>
    /// 读取项目配置
    /// </summary>
    public T? ReadProjectConfig<T>(Guid userId, Guid projectId) where T : class
    {
        var projectPath = GetProjectWorkspacePath(userId, projectId);
        var configPath = Path.Combine(projectPath, "project.json");

        if (!File.Exists(configPath))
        {
            return null;
        }

        var json = File.ReadAllText(configPath);
        return System.Text.Json.JsonSerializer.Deserialize<T>(json, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
}