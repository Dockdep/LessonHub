namespace LessonsHub.Infrastructure.Configuration;

public class LessonsAiApiSettings
{
    public string BaseUrl { get; set; } = "http://localhost:8000";
    public int TimeoutMinutes { get; set; } = 5;
    
    public AiPricingSettings Pricing { get; set; } = new();
}

public class AiPricingSettings
{
    public decimal GeminiProPreviewInputTokenPriceUnder200k { get; set; } = 0.000002m;
    public decimal GeminiProPreviewOutputTokenPriceUnder200k { get; set; } = 0.000012m;
    
    public decimal GeminiProPreviewInputTokenPriceOver200k { get; set; } = 0.000004m;
    public decimal GeminiProPreviewOutputTokenPriceOver200k { get; set; } = 0.000018m;
    
    public decimal GeminiFlashPreviewInputTokenPrice { get; set; } = 0.0000005m;
    public decimal GeminiFlashPreviewOutputTokenPrice { get; set; } = 0.000003m;
}
