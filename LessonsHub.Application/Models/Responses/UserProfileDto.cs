namespace LessonsHub.Application.Models.Responses;

public class UserProfileDto
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? PictureUrl { get; set; }
    public string? GoogleApiKey { get; set; }
}
