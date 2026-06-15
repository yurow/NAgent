namespace NAgent.Application.DTOs;

/// <summary>
/// 用户 DTO - 使用 camelCase 属性名以匹配前端 JavaScript
/// </summary>
public class UserDto
{
    public Guid id { get; set; }
    public string username { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public bool isActive { get; set; }
    public bool isAdmin { get; set; }
    public DateTime createdAt { get; set; }
    public DateTime? updatedAt { get; set; }
}
