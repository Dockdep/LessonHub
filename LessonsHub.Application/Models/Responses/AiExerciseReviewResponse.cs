using System.Text.Json.Serialization;

namespace LessonsHub.Application.Models.Responses;

public class AiExerciseReviewResponse : IModelUsageCarrier
{
    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }

    [JsonPropertyName("accuracyLevel")]
    public int AccuracyLevel { get; set; }

    [JsonPropertyName("examReview")]
    public string ExamReview { get; set; } = string.Empty;

    [JsonPropertyName("usage")]
    public List<ModelUsage> Usage { get; set; } = new();
}
