namespace BE_AI_Tourism.Infrastructure.Cloudinary;

public interface ICloudinaryProvider
{
    (string signature, long timestamp) GenerateSignature(string folder);
    Task<bool> DestroyAsync(string publicId);
}
