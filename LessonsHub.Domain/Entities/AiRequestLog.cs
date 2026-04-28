namespace LessonsHub.Domain.Entities;

public class AiRequestLog
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }
    public Guid? CorrelationId { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public decimal PricePerIn { get; set; }
    public decimal PricePerOut { get; set; }
    public decimal TotalCost { get; set; }
    public int LatencyMs { get; set; }
    public bool IsSuccess { get; set; }
    public string FinishReason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
