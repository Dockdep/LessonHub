using LessonsHub.Application.Abstractions.Repositories;
using LessonsHub.Domain.Entities;
using LessonsHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LessonsHub.Infrastructure.Repositories;

public sealed class LessonRepository : RepositoryBase, ILessonRepository
{
    public LessonRepository(LessonsHubDbContext db) : base(db) { }

    public Task<Lesson?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Lessons.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, ct);

    public Task<Lesson?> GetWithPlanAsync(int id, CancellationToken ct = default) =>
        _db.Lessons
            .Include(l => l.LessonPlan)
            .FirstOrDefaultAsync(l => l.Id == id, ct);

    public Task<Lesson?> GetWithDetailsAsync(int id, int forUserId, CancellationToken ct = default) =>
        _db.Lessons
            .Include(l => l.Exercises.Where(e => e.UserId == forUserId))
                .ThenInclude(e => e.Answers)
            .Include(l => l.LessonPlan).ThenInclude(lp => lp.User)
            .Include(l => l.Videos)
            .Include(l => l.Books)
            .Include(l => l.Documentation)
            .FirstOrDefaultAsync(l => l.Id == id, ct);

    public Task<Lesson?> GetWithLessonAndPlanForExerciseAsync(int exerciseId, CancellationToken ct = default) =>
        _db.Exercises
            .Where(e => e.Id == exerciseId)
            .Select(e => e.Lesson)
            .Include(l => l.LessonPlan)
            .FirstOrDefaultAsync(ct);

    public Task<List<Lesson>> GetByPlanAsync(int planId, CancellationToken ct = default) =>
        _db.Lessons.Where(l => l.LessonPlanId == planId).ToListAsync(ct);

    public async Task<(Lesson? previous, Lesson? next)> GetAdjacentAsync(int? planId, int lessonNumber, CancellationToken ct = default)
    {
        if (planId == null) return (null, null);
        var siblings = _db.Lessons.AsNoTracking().Where(l => l.LessonPlanId == planId);

        var prev = await siblings
            .Where(l => l.LessonNumber < lessonNumber)
            .OrderByDescending(l => l.LessonNumber)
            .FirstOrDefaultAsync(ct);

        var next = await siblings
            .Where(l => l.LessonNumber > lessonNumber)
            .OrderBy(l => l.LessonNumber)
            .FirstOrDefaultAsync(ct);

        return (prev, next);
    }

    public void Add(Lesson lesson) => _db.Lessons.Add(lesson);

    public void RemoveRange(IEnumerable<Lesson> lessons) => _db.Lessons.RemoveRange(lessons);
}
