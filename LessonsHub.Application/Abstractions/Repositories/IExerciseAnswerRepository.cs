using LessonsHub.Domain.Entities;

namespace LessonsHub.Application.Abstractions.Repositories;

public interface IExerciseAnswerRepository : IRepository
{
    void Add(ExerciseAnswer answer);
}
