using BE_AI_Tourism.Configuration;
using Microsoft.Extensions.Options;

namespace BE_AI_Tourism.Application.Services.Auth;

public class PasswordService : IPasswordService
{
    private readonly SecurityOptions _securityOptions;

    public PasswordService(IOptions<SecurityOptions> securityOptions)
    {
        _securityOptions = securityOptions.Value;
    }

    public string Hash(string password)
    {
        if (_securityOptions.AllowPlaintextPassword)
            return password;

        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool Verify(string password, string hashedPassword)
    {
        if (_securityOptions.AllowPlaintextPassword)
            return password == hashedPassword;

        return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
    }
}
