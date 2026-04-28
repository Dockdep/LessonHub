using LessonsHub.Application.Interfaces;
using LessonsHub.Application.Models.Responses;
using LessonsHub.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LessonsHub.Tests.Controllers;

public class LessonPlanControllerTests
{
    private static Mock<ILessonsAiApiClient> Ai() => new();

    // ---------- GetLessonPlanDetail ----------

    [Fact]
    public async Task Owner_can_read_own_plan_with_isOwner_true()
    {
        using var db = new TestDb();
        var plan = db.SeedPlan();

        var result = await TestStack.LessonPlanController(db, db.Owner.Id, Ai().Object).GetLessonPlanDetail(plan.Id);

        var dto = (LessonPlanDetailDto)((OkObjectResult)result).Value!;
        Assert.True(dto.IsOwner);
        Assert.Equal(db.Owner.Name, dto.OwnerName);
    }

    [Fact]
    public async Task Borrower_can_read_shared_plan_with_isOwner_false()
    {
        using var db = new TestDb();
        var plan = db.SeedPlan(sharedWith: new[] { db.Borrower.Id });

        var result = await TestStack.LessonPlanController(db, db.Borrower.Id, Ai().Object).GetLessonPlanDetail(plan.Id);

        var dto = (LessonPlanDetailDto)((OkObjectResult)result).Value!;
        Assert.False(dto.IsOwner);
        Assert.Equal(db.Owner.Name, dto.OwnerName);
    }

    [Fact]
    public async Task Stranger_cannot_read_unshared_plan()
    {
        using var db = new TestDb();
        var plan = db.SeedPlan();

        var result = await TestStack.LessonPlanController(db, db.Stranger.Id, Ai().Object).GetLessonPlanDetail(plan.Id);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ---------- shared-with-me ----------

    [Fact]
    public async Task SharedWithMe_lists_only_plans_shared_with_caller()
    {
        using var db = new TestDb();
        db.SeedPlan(name: "Shared", sharedWith: new[] { db.Borrower.Id });
        db.SeedPlan(name: "OwnedByOther"); // not shared
        db.SeedPlan(ownerId: db.Stranger.Id, name: "OtherOwnerShared", sharedWith: new[] { db.Borrower.Id });

        var result = await TestStack.LessonPlanController(db, db.Borrower.Id, Ai().Object).GetSharedWithMe();

        var list = (List<LessonPlanSummaryDto>)((OkObjectResult)result).Value!;
        Assert.Equal(2, list.Count);
        Assert.All(list, p => Assert.False(p.IsOwner));
        Assert.Contains(list, p => p.Name == "Shared");
        Assert.Contains(list, p => p.Name == "OtherOwnerShared");
    }

    [Fact]
    public async Task SharedWithMe_returns_empty_when_nothing_shared()
    {
        using var db = new TestDb();
        db.SeedPlan(); // owner-only

        var result = await TestStack.LessonPlanController(db, db.Borrower.Id, Ai().Object).GetSharedWithMe();

        var list = (List<LessonPlanSummaryDto>)((OkObjectResult)result).Value!;
        Assert.Empty(list);
    }

    // Share CRUD tests live in LessonPlanShareControllerTests now.

    // ---------- Mutations are owner-only ----------

    [Fact]
    public async Task DeleteLessonPlan_returns_404_for_borrower()
    {
        using var db = new TestDb();
        var plan = db.SeedPlan(sharedWith: new[] { db.Borrower.Id });

        var result = await TestStack.LessonPlanController(db, db.Borrower.Id, Ai().Object).DeleteLessonPlan(plan.Id);

        Assert.IsType<NotFoundObjectResult>(result);
        Assert.True(await db.Context.LessonPlans.AnyAsync(lp => lp.Id == plan.Id));
    }

    [Fact]
    public async Task DeleteLessonPlan_succeeds_for_owner()
    {
        using var db = new TestDb();
        var plan = db.SeedPlan();

        var result = await TestStack.LessonPlanController(db, db.Owner.Id, Ai().Object).DeleteLessonPlan(plan.Id);

        Assert.IsType<OkObjectResult>(result);
        Assert.False(await db.Context.LessonPlans.AnyAsync(lp => lp.Id == plan.Id));
    }
}
