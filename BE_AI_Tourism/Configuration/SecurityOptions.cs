namespace BE_AI_Tourism.Configuration;

public class SecurityOptions
{
    public bool AllowPlaintextPassword { get; set; } = false;
    public string EnvironmentMode { get; set; } = "Production";
}
