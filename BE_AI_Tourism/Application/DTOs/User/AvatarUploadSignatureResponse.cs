namespace BE_AI_Tourism.Application.DTOs.User;

public class AvatarUploadSignatureResponse
{
    public string Signature { get; set; } = string.Empty;
    public long Timestamp { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string CloudName { get; set; } = string.Empty;
    public string Folder { get; set; } = string.Empty;
}
