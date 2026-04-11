namespace BE_AI_Tourism.Application.DTOs.User;

public class FinalizeAvatarUploadRequest
{
    public string PublicId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string SecureUrl { get; set; } = string.Empty;
}
