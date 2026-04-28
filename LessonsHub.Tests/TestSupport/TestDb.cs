using LessonsHub.Domain.Entities;
using LessonsHub.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LessonsHub.Tests.TestSupport;

/// <summary>
/// Builds a real SQLite-in-memory DbContext per test, applies the EF schema, and seeds a
/// couple of users. SQLite (rather than the EF InMemory provider) catches real SQL behaviour
/// like unique indexes and FK cascades.
/// </summary>
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection;
    public LessonsHubDbContext Context { get; }
    public User Owner { get; private set; } = null!;
    public User Borrower { get; private set; } = null!;
    public User Stranger { get; private set; } = null!;

    public TestDb()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LessonsHubDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new LessonsHubDbContext(options);
        Context.Database.EnsureCreated();

        SeedUsers();
    }

    private void SeedUsers()
    {
        Owner = new User { GoogleId = "g-owner", Email = "owner@example.com", Name = "Owner User", GoogleApiKey = "owner-key" };
        Borrower = new User { GoogleId = "g-borrower", Email = "borrower@example.com", Name = "Borrower User", GoogleApiKey = "borrower-key" };
        Stranger = new User { GoogleId = "g-stranger", Email = "stranger@example.com", Name = "Stranger" };

        Context.Users.AddRange(Owner, Borrower, Stranger);
        Context.SaveChanges();
    }

    public LessonPlan SeedPlan(int? ownerId = null, string name = "Test Plan", IEnumerable<int>? sharedWith = null)
    {
        var plan = new LessonPlan
        {
            Name = name,
            Topic = "Test Topic",
            Description = "Description",
            UserId = ownerId ?? Owner.Id,
            CreatedDate = DateTime.UtcNow
        };
        Context.LessonPlans.Add(plan);
        Context.SaveChanges();

        if (sharedWith != null)
        {
            foreach (var userId in sharedWith)
            {
                Context.LessonPlanShares.Add(new LessonPlanShare
                {
                    LessonPlanId = plan.Id,
                    UserId = userId,
                    SharedAt = DateTime.UtcNow
                });
            }
            Context.SaveChanges();
        }

        return plan;
    }

    public Lesson SeedLesson(int planId, int lessonNumber = 1, string content = "Lesson content")
    {
        var lesson = new Lesson
        {
            LessonNumber = lessonNumber,
            Name = $"Lesson {lessonNumber}",
            ShortDescription = "Short",
            Content = content,
            LessonType = "Technical",
            LessonTopic = "Topic",
            LessonPlanId = planId
        };
        Context.Lessons.Add(lesson);
        Context.SaveChanges();
        return lesson;
    }

    public Exercise SeedExercise(int lessonId, int userId, string text = "Solve x")
    {
        var ex = new Exercise
        {
            LessonId = lessonId,
            UserId = userId,
            ExerciseText = text,
            Difficulty = "easy"
        };
        Context.Exercises.Add(ex);
        Context.SaveChanges();
        return ex;
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
