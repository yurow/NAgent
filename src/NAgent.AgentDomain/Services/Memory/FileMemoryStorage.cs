using System.Text.Json;

namespace NAgent.AgentDomain.Services.Memory;

/// <summary>
/// 基于文件的记忆存储实现
/// </summary>
public class FileMemoryStorage : IMemoryStorage
{
    private readonly string _workspaceBasePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public FileMemoryStorage(string workspaceBasePath)
    {
        _workspaceBasePath = workspaceBasePath ?? throw new ArgumentNullException(nameof(workspaceBasePath));
        Directory.CreateDirectory(_workspaceBasePath);
    }

    private string GetProjectPath(Guid projectId)
    {
        return Path.Combine(_workspaceBasePath, projectId.ToString());
    }

    private string GetSessionPath(Guid projectId, Guid sessionId)
    {
        return Path.Combine(GetProjectPath(projectId), "sessions", sessionId.ToString());
    }

    private string GetMemoryFilePath(Guid projectId, Guid sessionId)
    {
        return Path.Combine(GetSessionPath(projectId, sessionId), "memory.json");
    }

    public async Task SaveAsync(Guid projectId, Guid sessionId, SessionMemoryContext context, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var sessionPath = GetSessionPath(projectId, sessionId);
            Directory.CreateDirectory(sessionPath);

            var filePath = GetMemoryFilePath(projectId, sessionId);
            var json = JsonSerializer.Serialize(context, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await File.WriteAllTextAsync(filePath, json, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<SessionMemoryContext?> LoadAsync(Guid projectId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var filePath = GetMemoryFilePath(projectId, sessionId);

        if (!File.Exists(filePath))
        {
            return null;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var context = JsonSerializer.Deserialize<SessionMemoryContext>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return context;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteAsync(Guid projectId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var sessionPath = GetSessionPath(projectId, sessionId);
            if (Directory.Exists(sessionPath))
            {
                Directory.Delete(sessionPath, true);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<MemoryEntry>> SearchAsync(Guid projectId, string query, int limit = 10, CancellationToken cancellationToken = default)
    {
        var results = new List<MemoryEntry>();
        var projectPath = GetProjectPath(projectId);
        var sessionsPath = Path.Combine(projectPath, "sessions");

        if (!Directory.Exists(sessionsPath))
        {
            return results;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var sessionDirs = Directory.GetDirectories(sessionsPath);
            foreach (var sessionDir in sessionDirs)
            {
                var memoryFile = Path.Combine(sessionDir, "memory.json");
                if (File.Exists(memoryFile))
                {
                    var json = await File.ReadAllTextAsync(memoryFile, cancellationToken);
                    var context = JsonSerializer.Deserialize<SessionMemoryContext>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (context != null)
                    {
                        var matchingEntries = context.ShortTermMemory
                            .Concat(context.LongTermMemory)
                            .Where(m => m.Content.Contains(query, StringComparison.OrdinalIgnoreCase))
                            .Take(limit - results.Count);

                        results.AddRange(matchingEntries);

                        if (results.Count >= limit)
                            break;
                    }
                }
            }
        }
        finally
        {
            _lock.Release();
        }

        return results;
    }

    public async Task<List<SessionMemoryContext>> GetProjectMemoriesAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var memories = new List<SessionMemoryContext>();
        var projectPath = GetProjectPath(projectId);
        var sessionsPath = Path.Combine(projectPath, "sessions");

        if (!Directory.Exists(sessionsPath))
        {
            return memories;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var sessionDirs = Directory.GetDirectories(sessionsPath);
            foreach (var sessionDir in sessionDirs)
            {
                var memoryFile = Path.Combine(sessionDir, "memory.json");
                if (File.Exists(memoryFile))
                {
                    var json = await File.ReadAllTextAsync(memoryFile, cancellationToken);
                    var context = JsonSerializer.Deserialize<SessionMemoryContext>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (context != null)
                    {
                        memories.Add(context);
                    }
                }
            }
        }
        finally
        {
            _lock.Release();
        }

        return memories;
    }
}