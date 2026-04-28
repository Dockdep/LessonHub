namespace LessonsHub.Application.Models.Responses;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public LoginUserDto User { get; set; } = new();
}

public class LoginUserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? PictureUrl { get; set; }
}
