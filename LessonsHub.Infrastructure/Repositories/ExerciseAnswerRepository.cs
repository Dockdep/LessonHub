using LessonsHub.Application.Abstractions.Repositories;
using LessonsHub.Domain.Entities;
using LessonsHub.Infrastructure.Data;

namespace LessonsHub.Infrastructure.Repositories;

public sealed class ExerciseAnswerRepository : RepositoryBase, IExerciseAnswerRepository
{
    public ExerciseAnswerRepository(LessonsHubDbContext db) : base(db) { }

    public void Add(ExerciseAnswer answer) => _db.ExerciseAnswers.Add(answer);
}
