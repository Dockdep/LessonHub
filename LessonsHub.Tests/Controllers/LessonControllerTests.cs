using LessonsHub.Application.Interfaces;
using LessonsHub.Application.Models.Requests;
using LessonsHub.Application.Models.Responses;
using LessonsHub.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LessonsHub.Tests.Controllers;

public class LessonControllerTests
{
    private static (LessonsHub.Controllers.LessonController Controller, Mock<ILessonsAiApiClient> Ai) Build(TestDb db, int actingAs)
    {
        var ai = new Mock<ILessonsAiApiClient>();
        var controller = TestStack.LessonController(db, actingAs, ai.Object);
        return (controller, ai);
    }

    // ---------- Read access ----------

    [Fact]
    public async Task Owner_can_read_lesson_with_isOwner_true()
    {
        using var db = new TestDb();
        var plan = db.SeedPlan();
        var lesson = db.SeedLesson(plan.Id);
        var (controller, _) = Build(db, db.Owner.Id);

        var result = await controller.GetLesson(lesson.Id);

        var returned = (LessonDetailDto)((OkObjectResult)result).Value!;
        Assert.True(returned.IsOwner);
        Assert.Equal(db.Owner.Name, returned.OwnerName);
    }

    [Fact]
    public async Task Borrower_can_read_shared_lesson_with_isOwner_false()
    {
        using var db = new TestDb();
        var plan = db.SeedPlan(sharedWith: new[] { db.Borrower.Id });
        var lesson = db.SeedLesson(plan.Id);
        var (controller, _) = Build(db, db.Borrower.Id);

        var result = await controller.GetLesson(lesson.Id);

        var returned = (LessonDetailDto)((OkObjectResult)result).Value!;
        Assert.False(returned.IsOwner);
    }

    [Fact]
    public async Task Stranger_cannot_read_unshared_lesson()
    {
        using var db = new TestDb();
        var plan = db.SeedPlan();
        var lesson = db.SeedLesson(plan.Id);
        var (controller, _) = Build(db, db.Stranger.Id);

        var result = await controller.GetLesson(lesson.Id);

        Assert.IsType<NotFoundResult>(result);
    }

    // ---------- Per-user exercise filtering ----------

    [Fact]
    public async Task GetLesson_only_returns_callers_own_exercises()
    {
        using var db = new TestDb();
        var plan = db.SeedPlan(sharedWith: new[] { db.Borrower.Id });
        var lesson = db.SeedLesson(plan.Id);
        db.SeedExercise(lesson.Id, db.Owner.Id, "owner-exercise");
        db.SeedExercise(lesson.Id, db.Borrower.Id, "borrower-exercise");

        // In production each request uses a fresh DbContext; in tests the seeded entities
        // remain tracked and EF would otherwise fix them up onto the included collection.
        db.Context.ChangeTracker.Clear();

        var (ownerController, _) = Build(db, db.Owner.Id);
        var ownerResult = await ownerController.GetLesson(lesson.Id);
        var ownerLesson = (LessonDetailDto)((OkObjectResult)ownerResult).Value!;

        Assert.Single(ownerLesson.Exercises);
        Assert.Equal("owner-exercise", ownerLesson.Exercises[0].ExerciseText);

        // Re-clear and act as borrower
        db.Context.ChangeTracker.Clear();
        var (borrowerController, _) = Build(db, db.Borrower.Id);
        var borrowerResult = await borrowerController.GetLesson(lesson.Id);
        var borrowerLesson = (LessonDetailDto)((OkObjectResult)borrowerResult).Value!;

        Assert.Single(borrowerLesson.Exercises);
        Assert.Equal("borrower-exercise", borrowerLesson.Exercises[0].ExerciseText);
    }

    // ---------- Generate / Retry: borrower allowed, exercise tagged with caller ----------

    // Generate / Retry / Check now run inside JobBackgroundService via an
    // executor that calls the service method. Controller-level assertions
    // only verify the validate-then-enqueue path (4xx for invalid, 202 for
    // valid). The AI-call + DB-write behavior is tested directly against
    // the service.

    [Fact]
    public async Task GenerateExercise_borrower_can_generate_and_owns_the_exercise()
    {
        using var db = new TestDb();
        var plan = db.SeedPlan(sharedWith: new[] { db.Borrower.Id });
        var lesson = db.SeedLesson(plan.Id, content: "Has content");
        var ai = new Mock<ILessonsAiApiClient>();
        ai.Setup(x => x.GenerateLessonExerciseAsync(It.IsAny<AiLessonExerciseRequest>()))
            .ReturnsAsync(new AiLessonExerciseResponse { Exercise = "Generated text" });

        var service = TestStack.BuildExerciseServiceForTests(db, db.Borrower.Id, ai.Object);
        var result = await service.GenerateAsync(lesson.Id, "medium", null);

        Assert.True(result.IsSuccess);
        var persisted = await db.Context.Exercises.AsNoTracking().FirstAsync(e => e.Id == result.Value!.Id);
        Assert.Equal("Generated text", persisted.ExerciseText);
        Assert.Equal(db.Borrower.Id, persisted.UserId);
    }

    [Fact]
    public async Task GenerateExercise_returns_404_for_stranger()
    {
        using var db = new TestDb();
        var plan = db.SeedPlan();
        var lesson = db.SeedLesson(plan.Id, content: "Has content");
        var (controller, _) = Build(db, db.Stranger.Id);

        var result = await controller.GenerateExercise(lesson.Id, "medium", null, null, default);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task RetryExercise_tags_new_exercise_with_caller()
    {
        using var db = new TestDb();
        var plan = db.SeedPlan(sharedWith: new[] { db.Borrower.Id });
        var lesson = db.SeedLesson(plan.Id, content: "Has content");
        var ai = new Mock<ILessonsAiApiClient>();
        ai.Setup(x => x.RetryLessonExerciseAsync(It.IsAny<AiExerciseRetryRequest>()))
            .ReturnsAsync(new AiLessonExerciseResponse { Exercise = "Retry text" });

        var service = TestStack.BuildExerciseServiceForTests(db, db.Borrower.Id, ai.Object);
        var result = await service.RetryAsync(lesson.Id, "medium", null, "needs work");

        Assert.True(result.IsSuccess);
        var persisted = await db.Context.Exercises.AsNoTracking().FirstAsync(e => e.Id == result.Value!.Id);
        Assert.Equal(db.Borrower.Id, persisted.UserId);
    }

    // ---------- Check exercise: must own the exercise ----------

    [Fact]
    public async Task CheckExerciseReview_returns_404_when_exercise_belongs_to_other_user()
    {
        using var db = new TestDb();
        var plan = db.SeedPlan(sharedWith: new[] { db.Borrower.Id });
        var lesson = db.SeedLesson(plan.Id);
        var ownerExercise = db.SeedExercise(lesson.Id, db.Owner.Id);

        var (controller, ai) = Build(db, db.Borrower.Id);

        var result = await controller.CheckExerciseReview(ownerExercise.Id, "my answer", null, default);

        Assert.IsType<NotFoundResult>(result);
        ai.Verify(x => x.CheckExerciseReviewAsync(It.IsAny<AiExerciseReviewRequest>()), Times.Never);
    }

    [Fact]
    public async Task CheckExerciseReview_persists_answer_for_caller()
    {
        using var db = new TestDb();
        var plan = db.SeedPlan(sharedWith: new[] { db.Borrower.Id });
        var lesson = db.SeedLesson(plan.Id);
        var myExercise = db.SeedExercise(lesson.Id, db.Borrower.Id);

        var ai = new Mock<ILessonsAiApiClient>();
        ai.Setup(x => x.CheckExerciseReviewAsync(It.IsAny<AiExerciseReviewRequest>()))
            .ReturnsAsync(new AiExerciseReviewResponse { AccuracyLevel = 92, ExamReview = "Good" });

        var service = TestStack.BuildExerciseServiceForTests(db, db.Borrower.Id, ai.Object);
        var result = await service.CheckAnswerAsync(myExercise.Id, "answer");

        Assert.True(result.IsSuccess);
        Assert.Equal(myExercise.Id, result.Value!.ExerciseId);
        Assert.Equal(92, result.Value.AccuracyLevel);
    }

    // ---------- Owner-only mutations ----------

    [Fact]
    public async Task UpdateLesson_returns_404_for_borrower()
    {
        using var db = new TestDb();
        var plan = db.SeedPlan(sharedWith: new[] { db.Borrower.Id });
        var lesson = db.SeedLesson(plan.Id);
        var (controller, _) = Build(db, db.Borrower.Id);

        var result = await controller.UpdateLesson(lesson.Id, new UpdateLessonInfoDto
        {
            Name = "hijack",
            ShortDescription = "x",
            LessonTopic = "y",
            KeyPoints = new()
        });

        Assert.IsType<NotFoundResult>(result);
        var fresh = await db.Context.Lessons.AsNoTracking().FirstAsync(l => l.Id == lesson.Id);
        Assert.NotEqual("hijack", fresh.Name);
    }

    [Fact]
    public async Task ToggleLessonComplete_returns_404_for_borrower()
    {
        using var db = new TestDb();
        var plan = db.SeedPlan(sharedWith: new[] { db.Borrower.Id });
        var lesson = db.SeedLesson(plan.Id);
        var (controller, _) = Build(db, db.Borrower.Id);

        var result = await controller.ToggleLessonComplete(lesson.Id);

        Assert.IsType<NotFoundResult>(result);
        var fresh = await db.Context.Lessons.AsNoTracking().FirstAsync(l => l.Id == lesson.Id);
        Assert.False(fresh.IsCompleted);
    }

    [Fact]
    public async Task ToggleLessonComplete_owner_toggles_state()
    {
        using var db = new TestDb();
        var plan = db.SeedPlan();
        var lesson = db.SeedLesson(plan.Id);
        var (controller, _) = Build(db, db.Owner.Id);

        var firstResult = await controller.ToggleLessonComplete(lesson.Id);
        var first = (LessonDetailDto)((OkObjectResult)firstResult).Value!;
        Assert.True(first.IsCompleted);
        Assert.NotNull(first.CompletedAt);

        var secondResult = await controller.ToggleLessonComplete(lesson.Id);
        var second = (LessonDetailDto)((OkObjectResult)secondResult).Value!;
        Assert.False(second.IsCompleted);
        Assert.Null(second.CompletedAt);
    }

    // ---------- Siblings ----------

    [Fact]
    public async Task GetSiblingLessonIds_works_for_borrower()
    {
        using var db = new TestDb();
        var plan = db.SeedPlan(sharedWith: new[] { db.Borrower.Id });
        var l1 = db.SeedLesson(plan.Id, lessonNumber: 1);
        var l2 = db.SeedLesson(plan.Id, lessonNumber: 2);
        var l3 = db.SeedLesson(plan.Id, lessonNumber: 3);
        var (controller, _) = Build(db, db.Borrower.Id);

        var result = await controller.GetSiblingLessonIds(l2.Id);
        var dto = (SiblingLessonsDto)((OkObjectResult)result).Value!;

        Assert.Equal(l1.Id, dto.PrevLessonId);
        Assert.Equal(l3.Id, dto.NextLessonId);
    }
}
