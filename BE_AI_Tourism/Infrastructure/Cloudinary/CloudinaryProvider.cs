using BE_AI_Tourism.Configuration;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace BE_AI_Tourism.Infrastructure.Cloudinary;

public class CloudinaryProvider : ICloudinaryProvider
{
    private readonly CloudinaryDotNet.Cloudinary _cloudinary;

    public CloudinaryProvider(IOptions<CloudinaryOptions> options)
    {
        var config = options.Value;
        var account = new Account(config.CloudName, config.ApiKey, config.ApiSecret);
        _cloudinary = new CloudinaryDotNet.Cloudinary(account);
    }

    public (string signature, long timestamp) GenerateSignature(string folder)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var parameters = new SortedDictionary<string, object>
        {
            { "folder", folder },
            { "timestamp", timestamp }
        };
        var signature = _cloudinary.Api.SignParameters(parameters);
        return (signature, timestamp);
    }

    public async Task<bool> DestroyAsync(string publicId)
    {
        var deleteParams = new DeletionParams(publicId);
        var result = await _cloudinary.DestroyAsync(deleteParams);
        return result.Result == "ok";
    }
}
