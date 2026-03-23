using BE_AI_Tourism.Domain.Enums;

namespace BE_AI_Tourism.Application.DTOs.Auth;

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public UserRole? Role { get; set; }
    public ContributorType? ContributorType { get; set; }
    public Guid? AdministrativeUnitId { get; set; }
    public List<Guid> CategoryIds { get; set; } = [];
}
