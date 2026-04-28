using LessonsHub.Application.Abstractions.Repositories;
using LessonsHub.Domain.Entities;
using LessonsHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LessonsHub.Infrastructure.Repositories;

public sealed class ExerciseRepository : RepositoryBase, IExerciseRepository
{
    public ExerciseRepository(LessonsHubDbContext db) : base(db) { }

    public Task<Exercise?> GetForUserWithLessonAsync(int exerciseId, int userId, CancellationToken ct = default) =>
        _db.Exercises
            .Include(e => e.Lesson).ThenInclude(l => l.LessonPlan)
            .FirstOrDefaultAsync(e => e.Id == exerciseId && e.UserId == userId, ct);

    public void Add(Exercise exercise) => _db.Exercises.Add(exercise);
}
