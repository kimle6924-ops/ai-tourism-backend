using BE_AI_Tourism.Domain.Enums;

namespace BE_AI_Tourism.Application.DTOs.User;

public class UserResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; }
}
