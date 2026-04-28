using System.Text.Json.Serialization;

namespace LessonsHub.Application.Models.Responses;

public class AiLessonResourcesResponse : IModelUsageCarrier
{
    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }

    [JsonPropertyName("lessonName")]
    public string LessonName { get; set; } = string.Empty;

    [JsonPropertyName("topic")]
    public string Topic { get; set; } = string.Empty;

    [JsonPropertyName("videos")]
    public List<VideoResource> Videos { get; set; } = new();

    [JsonPropertyName("books")]
    public List<BookResource> Books { get; set; } = new();

    [JsonPropertyName("documentation")]
    public List<DocumentationResource> Documentation { get; set; } = new();

    [JsonPropertyName("usage")]
    public List<ModelUsage> Usage { get; set; } = new();
}

public class VideoResource
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("channel")]
    public string Channel { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

public class BookResource
{
    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("bookName")]
    public string BookName { get; set; } = string.Empty;

    [JsonPropertyName("chapterNumber")]
    public int? ChapterNumber { get; set; }

    [JsonPropertyName("chapterName")]
    public string? ChapterName { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

public class DocumentationResource
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("section")]
    public string? Section { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}
