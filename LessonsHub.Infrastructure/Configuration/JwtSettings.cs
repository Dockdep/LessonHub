namespace LessonsHub.Infrastructure.Configuration;

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "LessonsHub";
    public string Audience { get; set; } = "LessonsHub";
    public int ExpirationMinutes { get; set; } = 1440;
}
