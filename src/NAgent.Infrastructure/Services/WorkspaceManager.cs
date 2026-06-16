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
    /// 确保项目工作目录存在，并返回路径
    /// </summary>
    public string EnsureProjectWorkspace(Guid userId, Guid projectId)
    {
        return GetProjectWorkspacePath(userId, projectId);
    }

    /// <summary>
    /// 检查项目是否已初始化（init.md 是否存在）
    /// </summary>
    public bool IsInitialized(Guid userId, Guid projectId)
    {
        var projectPath = GetProjectWorkspacePath(userId, projectId);
        var initPath = Path.Combine(projectPath, "init.md");
        return File.Exists(initPath);
    }

    /// <summary>
    /// 获取相对工作目录路径（用于显示）
    /// </summary>
    public string GetRelativePath(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath)) return "";
        var fullWorkspace = Path.GetFullPath(_workspaceBasePath);
        var fullTarget = Path.GetFullPath(fullPath);
        if (fullTarget.StartsWith(fullWorkspace, StringComparison.OrdinalIgnoreCase))
        {
            return fullTarget[fullWorkspace.Length..].TrimStart(Path.DirectorySeparatorChar);
        }
        return fullPath;
    }

    /// <summary>
    /// 确保 init.md 存在，不存在则创建（标记项目已初始化）
    /// </summary>
    public string EnsureInitFile(Guid userId, Guid projectId, string projectName)
    {
        var projectPath = GetProjectWorkspacePath(userId, projectId);
        var initPath = Path.Combine(projectPath, "init.md");
        var relativePath = GetRelativePath(projectPath);

        if (!File.Exists(initPath))
        {
            var content = $"# 项目初始化\n\n" +
                         $"**项目名称**: {projectName}\n" +
                         $"**项目ID**: {projectId}\n" +
                         $"**用户ID**: {userId}\n" +
                         $"**初始化时间**: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n" +
                         $"**工作目录**: workspace/{relativePath}\n\n" +
                         $"系统已初始化完成。工作目录已创建。\n";
            File.WriteAllText(initPath, content);
        }

        return initPath;
    }

    /// <summary>
    /// 检查 spec.md 是否存在，不存在则创建
    /// </summary>
    public string EnsureSpecFile(Guid userId, Guid projectId)
    {
        var projectPath = GetProjectWorkspacePath(userId, projectId);
        var specPath = Path.Combine(projectPath, "spec.md");
        var relativePath = GetRelativePath(projectPath);

        if (!File.Exists(specPath))
        {
            var header = $"# 项目规范文档\n\n" +
                        $"**项目ID**: {projectId}\n" +
                        $"**创建时间**: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n" +
                        $"**工作目录**: workspace/{relativePath}\n\n" +
                        $"## 对话记录\n\n" +
                        $"本文档记录用户与 AI Agent 的所有对话，用于追踪项目需求和流程。\n\n";
            File.WriteAllText(specPath, header);
        }

        return specPath;
    }

    /// <summary>
    /// 写入 spec.md 完整内容（用于初始化时生成）
    /// </summary>
    public void WriteSpecFile(Guid userId, Guid projectId, string content)
    {
        var projectPath = GetProjectWorkspacePath(userId, projectId);
        var specPath = Path.Combine(projectPath, "spec.md");
        File.WriteAllText(specPath, content);
    }

    /// <summary>
    /// 将用户问题追加到 spec.md
    /// </summary>
    public void AppendQuestionToSpec(Guid userId, Guid projectId, string question, string? answer = null)
    {
        var specPath = EnsureSpecFile(userId, projectId);
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        var entry = $"### [{timestamp}] 用户提问\n\n" +
                   $"**问题**: {question}\n\n";

        if (!string.IsNullOrEmpty(answer))
        {
            entry += $"**AI回复**: {answer}\n\n";
        }

        entry += "---\n\n";

        File.AppendAllText(specPath, entry);
    }

    /// <summary>
    /// 读取 spec.md 内容
    /// </summary>
    public string ReadSpecFile(Guid userId, Guid projectId)
    {
        var specPath = EnsureSpecFile(userId, projectId);
        return File.ReadAllText(specPath);
    }

    /// <summary>
    /// 获取工作目录下的文件列表（相对路径）
    /// </summary>
    public List<string> GetWorkspaceFiles(Guid userId, Guid projectId)
    {
        var projectPath = GetProjectWorkspacePath(userId, projectId);
        if (!Directory.Exists(projectPath))
        {
            return new List<string>();
        }

        return Directory.GetFiles(projectPath, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(projectPath, f).Replace('\\', '/'))
            .ToList();
    }

    /// <summary>
    /// 获取项目工作空间的相对路径（相对于工作空间根目录）
    /// </summary>
    public string GetProjectRelativePath(Guid userId, Guid projectId)
    {
        var projectPath = GetProjectWorkspacePath(userId, projectId);
        return Path.GetRelativePath(_workspaceBasePath, projectPath).Replace('\\', '/');
    }

    /// <summary>
    /// 获取工作空间基础路径
    /// </summary>
    public string GetWorkspaceBasePath()
    {
        return _workspaceBasePath;
    }

    /// <summary>
    /// 获取项目记忆目录路径（临时记忆，按日期组织）
    /// 目录结构: workspace/{userId}/{projectId}/memory/
    /// </summary>
    public string GetMemoryDirectory(Guid userId, Guid projectId)
    {
        var projectPath = GetProjectWorkspacePath(userId, projectId);
        var memoryPath = Path.Combine(projectPath, "memory");
        Directory.CreateDirectory(memoryPath);
        return memoryPath;
    }

    /// <summary>
    /// 记录 LLM 调用到记忆文件（以日期结尾的 JSONL 文件）
    /// 每天一个文件，每行一条记录（JSONL 格式便于追加）
    /// </summary>
    public void RecordLlmCall(Guid userId, Guid projectId, string callType, string modelId, string prompt, string response, long durationMs, string? errorMessage = null)
    {
        try
        {
            var memoryDir = GetMemoryDirectory(userId, projectId);
            var dateStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var filePath = Path.Combine(memoryDir, $"llm-calls-{dateStr}.jsonl");

            var record = new
            {
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss:sss") + " UTC",
                call_type = callType,
                model_id = modelId,
                prompt_length = prompt?.Length ?? 0,
                prompt_preview = prompt != null && prompt.Length > 500 ? prompt[..500] + "...[截断]" : prompt,
                response_length = response?.Length ?? 0,
                response_preview = response != null && response.Length > 1000 ? response[..1000] + "...[截断]" : response,
                duration_ms = durationMs,
                error = errorMessage,
                project_id = projectId,
                user_id = userId
            };

            var json = System.Text.Json.JsonSerializer.Serialize(record);
            var line = json + "\n";

            File.AppendAllText(filePath, line);
        }
        catch
        {
            // 记录失败不影响主流程
        }
    }
}