namespace BE_AI_Tourism.Application.Services.Auth;

public interface IPasswordService
{
    string Hash(string password);
    bool Verify(string password, string hashedPassword);
}
