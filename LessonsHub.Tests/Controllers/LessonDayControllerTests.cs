using LessonsHub.Application.Models.Requests;
using LessonsHub.Application.Models.Responses;
using LessonsHub.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LessonsHub.Tests.Controllers;

public class LessonDayControllerTests
{
    [Fact]
    public async Task GetLessonPlans_only_returns_caller_owned_plans_with_isOwner_true()
    {
        using var db = new TestDb();
        db.SeedPlan(name: "Mine");
        db.SeedPlan(ownerId: db.Borrower.Id, name: "TheirsSharedToMe", sharedWith: new[] { db.Owner.Id });

        var result = await TestStack.LessonDayController(db, db.Owner.Id).GetLessonPlans();

        var list = (List<LessonPlanSummaryDto>)((OkObjectResult)result).Value!;
        Assert.Single(list);
        Assert.Equal("Mine", list[0].Name);
        Assert.True(list[0].IsOwner);
    }

    [Fact]
    public async Task AssignLesson_creates_per_user_LessonDay_row()
    {
        using var db = new TestDb();
        var ownerPlan = db.SeedPlan();
        var ownerLesson = db.SeedLesson(ownerPlan.Id);
        db.Context.ChangeTracker.Clear();

        var result = await TestStack.LessonDayController(db, db.Owner.Id).AssignLesson(new AssignLessonRequestDto
        {
            LessonId = ownerLesson.Id,
            Date = "2026-04-19",
            DayName = "Owner's day",
            DayDescription = "owner desc"
        });

        Assert.IsType<OkObjectResult>(result);
        var day = await db.Context.LessonDays.AsNoTracking().FirstAsync();
        Assert.Equal(db.Owner.Id, day.UserId);
        Assert.Equal("Owner's day", day.Name);
    }

    [Fact]
    public async Task AssignLesson_two_users_same_date_creates_two_isolated_days()
    {
        using var db = new TestDb();
        var ownerPlan = db.SeedPlan();
        var ownerLesson = db.SeedLesson(ownerPlan.Id);
        var borrowerPlan = db.SeedPlan(ownerId: db.Borrower.Id);
        var borrowerLesson = db.SeedLesson(borrowerPlan.Id);
        db.Context.ChangeTracker.Clear();

        await TestStack.LessonDayController(db, db.Owner.Id).AssignLesson(new AssignLessonRequestDto
        {
            LessonId = ownerLesson.Id,
            Date = "2026-05-01",
            DayName = "Owner's name",
            DayDescription = "owner"
        });
        db.Context.ChangeTracker.Clear();

        await TestStack.LessonDayController(db, db.Borrower.Id).AssignLesson(new AssignLessonRequestDto
        {
            LessonId = borrowerLesson.Id,
            Date = "2026-05-01",
            DayName = "Borrower's name",
            DayDescription = "borrower"
        });

        var days = await db.Context.LessonDays.AsNoTracking().OrderBy(d => d.UserId).ToListAsync();
        Assert.Equal(2, days.Count);
        Assert.Equal(db.Owner.Id, days[0].UserId);
        Assert.Equal("Owner's name", days[0].Name);
        Assert.Equal(db.Borrower.Id, days[1].UserId);
        Assert.Equal("Borrower's name", days[1].Name);
    }

    [Fact]
    public async Task AssignLesson_rejects_lesson_not_owned_by_caller()
    {
        using var db = new TestDb();
        var ownerPlan = db.SeedPlan(sharedWith: new[] { db.Borrower.Id });
        var lesson = db.SeedLesson(ownerPlan.Id);
        db.Context.ChangeTracker.Clear();

        // Borrower has read access to the lesson but assigning writes to Lesson.LessonDayId
        // (shared state) — controller currently restricts to plan owner.
        var result = await TestStack.LessonDayController(db, db.Borrower.Id).AssignLesson(new AssignLessonRequestDto
        {
            LessonId = lesson.Id,
            Date = "2026-05-01",
            DayName = "x",
            DayDescription = "x"
        });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetLessonDayByDate_does_not_leak_other_users_days()
    {
        using var db = new TestDb();
        var ownerPlan = db.SeedPlan();
        var ownerLesson = db.SeedLesson(ownerPlan.Id);
        var borrowerPlan = db.SeedPlan(ownerId: db.Borrower.Id);
        var borrowerLesson = db.SeedLesson(borrowerPlan.Id);
        db.Context.ChangeTracker.Clear();

        await TestStack.LessonDayController(db, db.Owner.Id).AssignLesson(new AssignLessonRequestDto
        {
            LessonId = ownerLesson.Id, Date = "2026-06-01", DayName = "secret-owner-name", DayDescription = ""
        });
        db.Context.ChangeTracker.Clear();
        await TestStack.LessonDayController(db, db.Borrower.Id).AssignLesson(new AssignLessonRequestDto
        {
            LessonId = borrowerLesson.Id, Date = "2026-06-01", DayName = "borrower-name", DayDescription = ""
        });
        db.Context.ChangeTracker.Clear();

        var borrowerView = await TestStack.LessonDayController(db, db.Borrower.Id).GetLessonDayByDate(new DateTime(2026, 6, 1));
        var dto = (LessonDayDto?)((OkObjectResult)borrowerView).Value;

        Assert.NotNull(dto);
        Assert.Equal("borrower-name", dto!.Name);
        Assert.DoesNotContain("secret-owner-name", dto.Name);
    }
}
