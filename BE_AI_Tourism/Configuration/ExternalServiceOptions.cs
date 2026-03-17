namespace BE_AI_Tourism.Configuration;

public class ExternalServiceOptions
{
    public string PaymentApiKey { get; set; } = string.Empty;
    public string AiServiceKey { get; set; } = string.Empty;
    public string PaymentBaseUrl { get; set; } = string.Empty;
    public string AiServiceBaseUrl { get; set; } = string.Empty;
}
