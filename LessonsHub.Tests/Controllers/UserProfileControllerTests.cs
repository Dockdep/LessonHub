using LessonsHub.Application.Models.Requests;
using LessonsHub.Application.Models.Responses;
using LessonsHub.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LessonsHub.Tests.Controllers;

public class UserProfileControllerTests
{
    [Fact]
    public async Task GetProfile_returns_current_users_data()
    {
        using var db = new TestDb();

        var result = await TestStack.UserProfileController(db, db.Owner.Id).GetProfile();

        var dto = Assert.IsType<UserProfileDto>(((OkObjectResult)result).Value);
        Assert.Equal(db.Owner.Email, dto.Email);
        Assert.Equal(db.Owner.Name, dto.Name);
        Assert.Equal(db.Owner.GoogleApiKey, dto.GoogleApiKey);
    }

    [Fact]
    public async Task GetProfile_for_different_users_returns_isolated_data()
    {
        using var db = new TestDb();

        var ownerResult = await TestStack.UserProfileController(db, db.Owner.Id).GetProfile();
        var borrowerResult = await TestStack.UserProfileController(db, db.Borrower.Id).GetProfile();

        var ownerDto = (UserProfileDto)((OkObjectResult)ownerResult).Value!;
        var borrowerDto = (UserProfileDto)((OkObjectResult)borrowerResult).Value!;

        Assert.NotEqual(ownerDto.Email, borrowerDto.Email);
        Assert.Equal("owner-key", ownerDto.GoogleApiKey);
        Assert.Equal("borrower-key", borrowerDto.GoogleApiKey);
    }

    [Fact]
    public async Task UpdateProfile_persists_new_api_key()
    {
        using var db = new TestDb();

        var result = await TestStack.UserProfileController(db, db.Owner.Id)
            .UpdateProfile(new UpdateUserProfileRequest { GoogleApiKey = "new-key-123" });

        var dto = (UserProfileDto)((OkObjectResult)result).Value!;
        Assert.Equal("new-key-123", dto.GoogleApiKey);

        var dbValue = await db.Context.Users.AsNoTracking().FirstAsync(u => u.Id == db.Owner.Id);
        Assert.Equal("new-key-123", dbValue.GoogleApiKey);
    }

    [Fact]
    public async Task UpdateProfile_treats_blank_string_as_null()
    {
        using var db = new TestDb();

        var result = await TestStack.UserProfileController(db, db.Owner.Id)
            .UpdateProfile(new UpdateUserProfileRequest { GoogleApiKey = "   " });

        var dto = (UserProfileDto)((OkObjectResult)result).Value!;
        Assert.Null(dto.GoogleApiKey);
    }

    [Fact]
    public async Task UpdateProfile_only_updates_current_user()
    {
        using var db = new TestDb();

        await TestStack.UserProfileController(db, db.Borrower.Id)
            .UpdateProfile(new UpdateUserProfileRequest { GoogleApiKey = "borrower-rotated" });

        var owner = await db.Context.Users.AsNoTracking().FirstAsync(u => u.Id == db.Owner.Id);
        var borrower = await db.Context.Users.AsNoTracking().FirstAsync(u => u.Id == db.Borrower.Id);

        Assert.Equal("owner-key", owner.GoogleApiKey);
        Assert.Equal("borrower-rotated", borrower.GoogleApiKey);
    }
}
