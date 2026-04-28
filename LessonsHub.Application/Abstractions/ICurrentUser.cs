namespace LessonsHub.Application.Abstractions;

public interface ICurrentUser
{
    int Id { get; }
    bool IsAuthenticated { get; }
}
