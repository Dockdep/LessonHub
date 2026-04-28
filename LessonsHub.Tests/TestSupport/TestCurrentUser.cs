using LessonsHub.Application.Abstractions;

namespace LessonsHub.Tests.TestSupport;

public sealed class TestCurrentUser : ICurrentUser
{
    public TestCurrentUser(int id) { Id = id; }
    public int Id { get; }
    public bool IsAuthenticated => true;
}
