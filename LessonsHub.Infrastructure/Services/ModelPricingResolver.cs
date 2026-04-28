using LessonsHub.Infrastructure.Configuration;

namespace LessonsHub.Infrastructure.Services;

/// <summary>
/// Resolves (pricePerInputToken, pricePerOutputToken) for a given model name + input-token count.
/// Adding a new model means appending a rule — no method modification needed (OCP).
/// </summary>
public class ModelPricingResolver
{
    private readonly List<PricingRule> _rules;

    public ModelPricingResolver(AiPricingSettings pricing)
    {
        // Order matters: first match wins. Place tighter (Pro under-200k vs over-200k) before
        // looser (Flash) and looser before catch-all (zero).
        _rules = new List<PricingRule>
        {
            new(ModelKeyword: "pro", MaxInputTokens: 200_000,
                PricePerIn: pricing.GeminiProPreviewInputTokenPriceUnder200k,
                PricePerOut: pricing.GeminiProPreviewOutputTokenPriceUnder200k),
            new(ModelKeyword: "pro", MaxInputTokens: int.MaxValue,
                PricePerIn: pricing.GeminiProPreviewInputTokenPriceOver200k,
                PricePerOut: pricing.GeminiProPreviewOutputTokenPriceOver200k),
            new(ModelKeyword: "flash", MaxInputTokens: int.MaxValue,
                PricePerIn: pricing.GeminiFlashPreviewInputTokenPrice,
                PricePerOut: pricing.GeminiFlashPreviewOutputTokenPrice),
        };
    }

    public (decimal PricePerIn, decimal PricePerOut) Resolve(string modelName, int inputTokens)
    {
        var lower = modelName.ToLowerInvariant();
        foreach (var rule in _rules)
        {
            if (lower.Contains(rule.ModelKeyword) && inputTokens <= rule.MaxInputTokens)
                return (rule.PricePerIn, rule.PricePerOut);
        }
        return (0m, 0m); // Unknown model — log it as free; bug surfaces in cost dashboards.
    }

    private sealed record PricingRule(string ModelKeyword, int MaxInputTokens, decimal PricePerIn, decimal PricePerOut);
}
