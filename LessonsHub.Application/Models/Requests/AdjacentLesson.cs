using System.Text.Json.Serialization;

namespace LessonsHub.Application.Models.Requests;

public class AdjacentLesson
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}
