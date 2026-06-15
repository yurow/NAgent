namespace NAgent.AgentDomain.Entities;

/// <summary>
/// Skill 实体 - 通过 MD 文档描述的能力
/// </summary>
public class Skill
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string Category { get; private set; }
    public string Version { get; private set; }
    public string Author { get; private set; }
    public string MarkdownContent { get; private set; }
    public string FilePath { get; private set; }
    public bool IsEnabled { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// Skill 包含的工具名称列表
    /// </summary>
    public List<string> ToolNames { get; private set; } = new();

    /// <summary>
    /// Skill 的示例用法
    /// </summary>
    public List<SkillExample> Examples { get; private set; } = new();

    private Skill() { }

    public Skill(string name, string description, string category, string markdownContent, string filePath)
    {
        Id = Guid.NewGuid();
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Category = category ?? "general";
        MarkdownContent = markdownContent ?? throw new ArgumentNullException(nameof(markdownContent));
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        Version = "1.0.0";
        Author = "system";
        IsEnabled = true;
        CreatedAt = DateTime.UtcNow;
        ToolNames = new List<string>();
        Examples = new List<SkillExample>();
    }

    public void UpdateContent(string markdownContent)
    {
        MarkdownContent = markdownContent ?? throw new ArgumentNullException(nameof(markdownContent));
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddToolName(string toolName)
    {
        if (!ToolNames.Contains(toolName))
            ToolNames.Add(toolName);
    }

    public void RemoveToolName(string toolName)
    {
        ToolNames.Remove(toolName);
    }

    public void AddExample(SkillExample example)
    {
        Examples.Add(example ?? throw new ArgumentNullException(nameof(example)));
    }

    public void UpdateMetadata(string version, string author)
    {
        Version = version ?? Version;
        Author = author ?? Author;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Skill 示例用法
/// </summary>
public class SkillExample
{
    public string Title { get; set; } = string.Empty;
    public string Input { get; set; } = string.Empty;
    public string Output { get; set; } = string.Empty;
    public string? Explanation { get; set; }
}
