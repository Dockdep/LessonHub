using LessonsHub.Domain.Entities;

namespace LessonsHub.Application.Interfaces;

public interface ITokenService
{
    string CreateToken(User user);
}
