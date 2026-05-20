namespace NAgent.Application.DTOs;

public record UserDto(
    Guid Id,
    string Username,
    string Email,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
