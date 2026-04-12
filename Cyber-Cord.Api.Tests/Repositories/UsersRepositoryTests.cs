using System.Drawing;
using Cyber_Cord.Api.Data;
using Cyber_Cord.Api.Entities;
using Cyber_Cord.Api.Models;
using Cyber_Cord.Api.Repositories;
using Cyber_Cord.Api.Tests.Helpers;

namespace Cyber_Cord.Api.Tests.Repositories;

public class UsersRepositoryTests : IDisposable
{
    // Testing removal of things is currently not possible, because we are using ExecuteDeleteAsync, which is not supported by InMemoryDatabase
    // Testing of creation and saving of settings also not supported by InMemoryDatabase

    private const int CurrentUserId = 1;
    private const string CurrentUserEmail = "current@example.org";
    private const int OtherUserId = 2;
    private const string OtherUserEmail = "other@example.org";
    private const int FriendshipId = 1;
    private const int OtherFriendshipId = 2;
    private const int RandomUserId = 3;
    private const string RandomUserEmail = "random@example.org";

    private readonly AppDbContext _context;
    private readonly UsersRepository _repository;

    public UsersRepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        var userManager = TestUserManagerFactory.Create(_context);

        _repository = new(_context, userManager);
    }

    public void Dispose() => _context.Dispose();

    private async Task SeedUserAsync(int id, string email, bool isActivated = true)
    {
        _context.Users.Add(new User
        {
            Id = id,
            Email = email,
            UserName = email,
            DisplayName = email.Split('@')[0],
            BannerColor = Color.Magenta,
            CreatedAt = DateTime.MaxValue,
            IsActivated = isActivated,
            Description = $"Description of {email}"
        });

        await _context.SaveChangesAsync();
    }

    private async Task SeedFriendshipAsync(int id, int rid, int lid, bool pending = false)
    {
        _context.Friendships.Add(new Friendship
        {
            Id = id,
            RequestingId = rid,
            ReceivingId = lid,
            Pending = pending,
        });

        await _context.SaveChangesAsync();
    }

    private async Task SeedRequestAsync(int id, int rid, int lid)
    {
        await SeedFriendshipAsync(id, rid, lid, true);
    }

    private async Task GiveUserSettingsAsync(int userId)
    {
        var settings = new Settings
        {
            UserId = userId,
            EnableSounds = true,
        };

        _context.Settings.Add(settings);

        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetUserByIdAsync_GetsUser()
    {
        await SeedUserAsync(CurrentUserId, CurrentUserEmail);

        var user = await _repository.GetUserByIdAsync(CurrentUserId);

        Assert.NotNull(user);
        Assert.Equal(
            user,
            await _context.Users.FindAsync(CurrentUserId)
            );
    }

    [Fact]
    public async Task GetUserByIdAsync_DoesNotFindUser()
    {
        await SeedUserAsync(CurrentUserId, CurrentUserEmail);

        var user = await _repository.GetUserByIdAsync(OtherUserId);

        Assert.Null(user);
    }

    [Fact]
    public async Task GetFriendshipByIdAsync_GetsFriendship()
    {
        await SeedUserAsync(CurrentUserId, CurrentUserEmail);
        await SeedUserAsync(OtherUserId, OtherUserEmail);
        await SeedFriendshipAsync(FriendshipId, CurrentUserId, OtherUserId);

        var friendship = await _repository.GetFriendshipByIdAsync(FriendshipId);

        Assert.NotNull(friendship);
        Assert.Equal(
            friendship,
            await _context.Friendships.FindAsync(FriendshipId)
            );
    }

    [Fact]
    public async Task GetFriendshipByIdAsync_Throws()
    {
        await SeedUserAsync(CurrentUserId, CurrentUserEmail);
        await SeedUserAsync(OtherUserId, OtherUserEmail);
        await SeedFriendshipAsync(FriendshipId, CurrentUserId, OtherUserId);

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _repository.GetFriendshipByIdAsync(OtherFriendshipId)
            );
    }

    [Fact]
    public async Task UserHasAccessToFriendshipAsync_IsTrue()
    {
        await SeedUserAsync(CurrentUserId, CurrentUserEmail);
        await SeedUserAsync(OtherUserId, OtherUserEmail);
        await SeedFriendshipAsync(FriendshipId, CurrentUserId, OtherUserId);

        var accessCurrent = await _repository.UserHasAccessToFriendshipAsync(CurrentUserId, FriendshipId);
        var accessOther = await _repository.UserHasAccessToFriendshipAsync(OtherUserId, FriendshipId);

        Assert.True(accessCurrent);
        Assert.True(accessOther);
    }

    [Fact]
    public async Task UserHasAccessToFriendshipAsync_Throws()
    {
        await SeedUserAsync(CurrentUserId, CurrentUserEmail);
        await SeedUserAsync(OtherUserId, OtherUserEmail);
        await SeedFriendshipAsync(FriendshipId, CurrentUserId, OtherUserId);

        var randomAccess = await _repository.UserHasAccessToFriendshipAsync(RandomUserId, FriendshipId);

        Assert.False(randomAccess);
    }

    [Fact]
    public async Task UserExistsAsync_ReturnsTrue()
    {
        await SeedUserAsync(CurrentUserId, CurrentUserEmail);

        var exists = await _repository.UserExistsAsync(CurrentUserId);

        Assert.True(exists);
    }

    [Fact]
    public async Task UserExistsAsync_ReturnsFalse()
    {
        await SeedUserAsync(CurrentUserId, CurrentUserEmail);

        var exists = await _repository.UserExistsAsync(OtherUserId);

        Assert.False(exists);
    }

    [Fact]
    public async Task UsersAreFriendsAsync_ReturnsTrue()
    {
        await SeedUserAsync(CurrentUserId, CurrentUserEmail);
        await SeedUserAsync(OtherUserId, OtherUserEmail);
        await SeedFriendshipAsync(FriendshipId, CurrentUserId, OtherUserId);

        var areFriends = await _repository.UsersAreFriendsAsync(CurrentUserId, OtherUserId);

        Assert.True(areFriends);
    }

    [Fact]
    public async Task UsersAreFriendsAsync_ReturnsFalse()
    {
        var areFriends = await _repository.UsersAreFriendsAsync(CurrentUserId, OtherUserId);

        Assert.False(areFriends);
    }

    [Fact]
    public async Task GetUserDetailAsync_GetsDetail()
    {
        await SeedUserAsync(CurrentUserId, CurrentUserEmail);

        var detail = await _repository.GetUserDetailAsync(CurrentUserId);

        var user = await _context.Users.FindAsync(CurrentUserId);
        Assert.NotNull(detail);
        Assert.Equal(CurrentUserId, detail.Id);
        Assert.NotNull(detail.BannerColor);
        Assert.Equal(user!.BannerColor.R, detail.BannerColor.Red);
        Assert.Equal(user.BannerColor.G, detail.BannerColor.Green);
        Assert.Equal(user.BannerColor.B, detail.BannerColor.Blue);
        Assert.Equal(user.UserName, detail.Name);
        Assert.Equal(user.DisplayName, detail.DisplayName);
        Assert.Equal(user.Email, detail.Email);
        Assert.Equal(user.IsActivated, detail.IsActivated);
        Assert.Equal(user.CreatedAt, detail.CreatedAt);
        Assert.Equal(user.Description, detail.Description);
    }

    [Fact]
    public async Task GetUserDetailAsync_DoesNotFindUser()
    {
        await SeedUserAsync(CurrentUserId, CurrentUserEmail);

        var detail = await _repository.GetUserDetailAsync(OtherUserId);

        Assert.Null(detail);
    }

    [Fact]
    public async Task GetUsersSettingsAsync_ReturnsUsersSettings()
    {
        await SeedUserAsync(CurrentUserId, CurrentUserEmail);
        await GiveUserSettingsAsync(CurrentUserId);

        var settings = await _repository.GetUsersSettingsAsync(CurrentUserId);

        Assert.NotNull(settings);
        Assert.Equal(CurrentUserId, settings.UserId);
    }

    [Fact]
    public async Task GetPendingRequestsAsync_GetsRequests()
    {
        await SeedUserAsync(CurrentUserId, CurrentUserEmail);
        await SeedUserAsync(OtherUserId, OtherUserEmail);
        await SeedRequestAsync(FriendshipId, CurrentUserId, OtherUserId);

        var currentRequests = await _repository.GetPendingRequestsAsync(CurrentUserId);
        var otherRequests = await _repository.GetPendingRequestsAsync(OtherUserId);

        Assert.Single(currentRequests);
        Assert.Single(otherRequests);
        Assert.Equal(FriendshipId, currentRequests[0].Id);
        Assert.Equal(FriendshipId, otherRequests[0].Id);
    }

    [Fact]
    public async Task GetPendingRequestsAsync_FindsNone()
    {
        await SeedUserAsync(CurrentUserId, CurrentUserEmail);
        await SeedUserAsync(OtherUserId, OtherUserEmail);
        await SeedRequestAsync(FriendshipId, CurrentUserId, OtherUserId);

        var randomRequests = await _repository.GetPendingRequestsAsync(RandomUserId);

        Assert.Empty(randomRequests);
    }

    [Fact]
    public async Task SearchMultipleAsync_FindsTwo()
    {
        await SeedUserAsync(CurrentUserId, CurrentUserEmail);
        await SeedUserAsync(OtherUserId, OtherUserEmail);

        var model = new UserSearchModel
        {
            SearchName = "example"
        };

        var results = await _repository.SearchMultipleAsync(model);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task SearchMultipleAsync_FindsOne()
    {
        await SeedUserAsync(CurrentUserId, CurrentUserEmail);
        await SeedUserAsync(OtherUserId, OtherUserEmail);

        var model = new UserSearchModel
        {
            SearchName = "example",
            Limit = 1
        };

        var results = await _repository.SearchMultipleAsync(model);

        Assert.Single(results);
    }

    [Fact]
    public async Task SearchMultipleAsync_FindsNone()
    {
        await SeedUserAsync(CurrentUserId, CurrentUserEmail);
        await SeedUserAsync(OtherUserId, OtherUserEmail);

        var model = new UserSearchModel
        {
            SearchName = "notexample"
        };

        var results = await _repository.SearchMultipleAsync(model);

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchSingularAsync_FindsOne()
    {
        await SeedUserAsync(CurrentUserId, CurrentUserEmail);
        await SeedUserAsync(OtherUserId, OtherUserEmail);

        var model = new UserSearchModel
        {
            SearchName = CurrentUserEmail,
            SingleResultOnly = true
        };

        var result = await _repository.SearchSingularAsync(model);

        Assert.NotNull(result);
        Assert.Equal(CurrentUserId, result.Id);
    }

    [Fact]
    public async Task SearchSingularAsync_FindsNone()
    {
        await SeedUserAsync(CurrentUserId, CurrentUserEmail);

        var model = new UserSearchModel
        {
            SearchName = "something",
            SingleResultOnly = true
        };

        var result = await _repository.SearchSingularAsync(model);

        Assert.Null(result);
    }

    [Fact]
    public async Task SearchMultipleFriendsAsync_FindsTwo()
    {
        await SeedUserAsync(CurrentUserId, CurrentUserEmail);
        await SeedUserAsync(OtherUserId, OtherUserEmail);
        await SeedUserAsync(RandomUserId, RandomUserEmail);
        await SeedFriendshipAsync(FriendshipId, CurrentUserId, OtherUserId);
        await SeedFriendshipAsync(OtherFriendshipId, OtherUserId, CurrentUserId);

        var model = new FriendSearchModel
        {
            SearchName = "example"
        };

        var results = await _repository.SearchMultipleFriendsAsync(CurrentUserId, model);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task SearchMultipleFriendsAsync_FindsOne()
    {
        await SeedUserAsync(CurrentUserId, CurrentUserEmail);
        await SeedUserAsync(OtherUserId, OtherUserEmail);
        await SeedUserAsync(RandomUserId, RandomUserEmail);
        await SeedFriendshipAsync(FriendshipId, CurrentUserId, OtherUserId);
        await SeedFriendshipAsync(OtherFriendshipId, OtherUserId, CurrentUserId);

        var model = new FriendSearchModel
        {
            SearchName = "example",
            Limit = 1
        };

        var results = await _repository.SearchMultipleFriendsAsync(CurrentUserId, model);

        Assert.Single(results);
    }

    [Fact]
    public async Task SearchMultipleFriendsAsync_FindsNone()
    {
        await SeedUserAsync(CurrentUserId, CurrentUserEmail);
        await SeedUserAsync(OtherUserId, OtherUserEmail);
        await SeedFriendshipAsync(FriendshipId, CurrentUserId, OtherUserId);

        var model = new FriendSearchModel
        {
            SearchName = "something"
        };

        var results = await _repository.SearchMultipleFriendsAsync(CurrentUserId, model);

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchSingularFriendsAsync_FindsOne()
    {
        await SeedUserAsync(CurrentUserId, CurrentUserEmail);
        await SeedUserAsync(OtherUserId, OtherUserEmail);
        await SeedFriendshipAsync(FriendshipId, CurrentUserId, OtherUserId);

        var currentResult   = await _repository.SearchSingularFriendsAsync(CurrentUserId, new() { SearchUserId = OtherUserId });
        var otherResult     = await _repository.SearchSingularFriendsAsync(OtherUserId, new() { SearchUserId = CurrentUserId });

        Assert.NotNull(currentResult.Item1);
        Assert.Null(currentResult.Item2);
        Assert.NotNull(otherResult.Item2);
        Assert.Null(otherResult.Item1);
        Assert.Equal(FriendshipId, currentResult.Item1.Id);
        Assert.Equal(FriendshipId, otherResult.Item2.Id);
    }

    [Fact]
    public async Task SearchSingularFriendsAsync_FindsNone()
    {
        await SeedUserAsync(CurrentUserId, CurrentUserEmail);
        await SeedUserAsync(OtherUserId, OtherUserEmail);
        await SeedFriendshipAsync(FriendshipId, CurrentUserId, OtherUserId);

        var currentResult = await _repository.SearchSingularFriendsAsync(CurrentUserId, new() { SearchUserId = RandomUserId });
        var otherResult = await _repository.SearchSingularFriendsAsync(OtherUserId, new() { SearchUserId = RandomUserId });

        Assert.Null(currentResult.Item1);
        Assert.Null(currentResult.Item2);
        Assert.Null(otherResult.Item1);
        Assert.Null(otherResult.Item2);
    }

    [Fact]
    public async Task CreateUserAsync_CreatesUser()
    {
        var model = new UserCreateModel
        {
            BannerColor = new()
            {
                Red = 0,
                Green = 0,
                Blue = 0,
            },
            Description = "description",
            DisplayName = "display name",
            Email = "name@example.org",
            Name = "name",
            Password = "password"
        };

        var user = await _repository.CreateUserAsync(model, model.Password);

        Assert.NotNull(user);
        Assert.NotNull(
            await _context.Users.FindAsync(user.Id)
            );
    }

    [Fact]
    public async Task ValidateUserAsync_ValidatesUser()
    {
        await SeedUserAsync(CurrentUserId, CurrentUserEmail, false);
        var user = await _context.Users.FindAsync(CurrentUserId);

        await _repository.ValidateUserAsync(user!);

        var result = await _context.Users.FindAsync(CurrentUserId);
        Assert.True(result!.IsActivated);
    }

    [Fact]
    public async Task RequestFriendshipAsync_RequestsFriendship()
    {
        await SeedUserAsync(CurrentUserId, CurrentUserEmail);
        await SeedUserAsync(OtherUserId, OtherUserEmail);

        var friendship = await _repository.RequestFriendshipAsync(CurrentUserId, new() { UserId = OtherUserId });

        Assert.NotNull(friendship);
        Assert.NotNull(await _context.Friendships.FindAsync(friendship.Id));
        Assert.Equal(CurrentUserId, friendship.RequestingId);
        Assert.Equal(OtherUserId, friendship.ReceivingId);
    }

    [Fact]
    public async Task AcceptFriendshipAsync_AcceptsFriendship()
    {
        await SeedUserAsync(CurrentUserId, CurrentUserEmail);
        await SeedUserAsync(OtherUserId, OtherUserEmail);
        await SeedRequestAsync(FriendshipId, CurrentUserId, OtherUserId);
        var friendRequest = await _context.Friendships.FindAsync(FriendshipId);

        await _repository.AcceptFriendshipAsync(friendRequest!);

        var result = await _context.Friendships.FindAsync(FriendshipId);
        Assert.False(result!.Pending);
    }

    [Fact]
    public async Task UpdateUserAsync_UpdatesUser()
    {
        await SeedUserAsync(CurrentUserId, CurrentUserEmail);

        var model = new UserUpdateModel
        {
            BannerColor = new()
            {
                Red = 0,
                Green = 0,
                Blue = 0
            },
            Description = "SOME_DESCRIPTION",
            DisplayName = "SOME_DISPLAYNAME"
        };

        var user = await _context.Users.FindAsync(CurrentUserId);

        var result = await _repository.UpdateUserAsync(user!, model);

        Assert.Equal(model.BannerColor.Red, result.BannerColor.Red);
        Assert.Equal(model.BannerColor.Green, result.BannerColor.Green);
        Assert.Equal(model.BannerColor.Blue, result.BannerColor.Blue);
        Assert.Equal(model.Description, result.Description);
        Assert.Equal(model.DisplayName, result.DisplayName);
    }

    [Fact]
    public async Task ChangeUserPassword_ChangesUserPassword()
    {
        await SeedUserAsync(CurrentUserId, CurrentUserEmail);
        var user = await _context.Users.FindAsync(CurrentUserId);

        await _repository.ChangeUserPasswordAsync(user!, "HASH");

        var result = await _context.Users.FindAsync(CurrentUserId);
        Assert.Equal("HASH", result!.PasswordHash);
    }
}
