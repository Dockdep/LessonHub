using LessonsHub.Application.Models.Responses;
using LessonsHub.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LessonsHub.Tests.Controllers;

public class LessonPlanShareControllerTests
{
    [Fact]
    public async Task AddShare_creates_share_for_existing_user()
    {
        using var db = new TestDb();
        var plan = db.SeedPlan();

        var result = await TestStack.ShareController(db, db.Owner.Id)
            .AddShare(plan.Id, new AddShareRequestDto { Email = db.Borrower.Email });

        var share = (LessonPlanShareDto)((OkObjectResult)result).Value!;
        Assert.Equal(db.Borrower.Id, share.UserId);

        var persisted = await db.Context.LessonPlanShares
            .AnyAsync(s => s.LessonPlanId == plan.Id && s.UserId == db.Borrower.Id);
        Assert.True(persisted);
    }

    [Fact]
    public async Task AddShare_returns_404_when_email_unknown()
    {
        using var db = new TestDb();
        var plan = db.SeedPlan();

        var result = await TestStack.ShareController(db, db.Owner.Id)
            .AddShare(plan.Id, new AddShareRequestDto { Email = "ghost@nowhere.com" });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task AddShare_returns_409_when_already_shared()
    {
        using var db = new TestDb();
        var plan = db.SeedPlan(sharedWith: new[] { db.Borrower.Id });

        var result = await TestStack.ShareController(db, db.Owner.Id)
            .AddShare(plan.Id, new AddShareRequestDto { Email = db.Borrower.Email });

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task AddShare_rejects_sharing_with_self()
    {
        using var db = new TestDb();
        var plan = db.SeedPlan();

        var result = await TestStack.ShareController(db, db.Owner.Id)
            .AddShare(plan.Id, new AddShareRequestDto { Email = db.Owner.Email });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task AddShare_returns_404_when_caller_is_not_owner()
    {
        using var db = new TestDb();
        var plan = db.SeedPlan(sharedWith: new[] { db.Borrower.Id });

        // Borrower tries to re-share
        var result = await TestStack.ShareController(db, db.Borrower.Id)
            .AddShare(plan.Id, new AddShareRequestDto { Email = db.Stranger.Email });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task RemoveShare_deletes_existing_share()
    {
        using var db = new TestDb();
        var plan = db.SeedPlan(sharedWith: new[] { db.Borrower.Id });

        var result = await TestStack.ShareController(db, db.Owner.Id).RemoveShare(plan.Id, db.Borrower.Id);

        Assert.IsType<OkObjectResult>(result);
        Assert.False(await db.Context.LessonPlanShares.AnyAsync(s => s.LessonPlanId == plan.Id));
    }

    [Fact]
    public async Task GetShares_returns_404_for_non_owner()
    {
        using var db = new TestDb();
        var plan = db.SeedPlan(sharedWith: new[] { db.Borrower.Id });

        var result = await TestStack.ShareController(db, db.Borrower.Id).GetShares(plan.Id);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetShares_lists_share_recipients_for_owner()
    {
        using var db = new TestDb();
        var plan = db.SeedPlan(sharedWith: new[] { db.Borrower.Id, db.Stranger.Id });

        var result = await TestStack.ShareController(db, db.Owner.Id).GetShares(plan.Id);

        var shares = (List<LessonPlanShareDto>)((OkObjectResult)result).Value!;
        Assert.Equal(2, shares.Count);
        Assert.Contains(shares, s => s.Email == db.Borrower.Email);
        Assert.Contains(shares, s => s.Email == db.Stranger.Email);
    }
}
