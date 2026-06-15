using NAgent.AgentDomain.Entities;

namespace NAgent.AgentDomain.Services;

/// <summary>
/// Skill Markdown 文档解析器接口
/// </summary>
public interface ISkillParser
{
    /// <summary>
    /// 从 Markdown 内容解析 Skill
    /// </summary>
    Skill Parse(string markdownContent, string filePath);

    /// <summary>
    /// 从文件解析 Skill
    /// </summary>
    Task<Skill> ParseFromFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证 Markdown 内容是否符合 Skill 规范
    /// </summary>
    bool Validate(string markdownContent, out string? errorMessage);
}
