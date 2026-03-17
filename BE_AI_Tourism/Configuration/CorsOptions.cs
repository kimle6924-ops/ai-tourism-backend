namespace BE_AI_Tourism.Configuration;

public class CorsOptions
{
    public string[] AllowedOrigins { get; set; } = [];
    public string[] AllowedMethods { get; set; } = ["GET", "POST", "PUT", "DELETE", "PATCH"];
    public string[] AllowedHeaders { get; set; } = ["Content-Type", "Authorization"];
    public bool AllowCredentials { get; set; } = true;
}
