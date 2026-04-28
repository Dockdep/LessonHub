namespace LessonsHub.Application.Models.Requests;

public interface IAiRequestWithApiKey
{
    string? GoogleApiKey { get; set; }
}
