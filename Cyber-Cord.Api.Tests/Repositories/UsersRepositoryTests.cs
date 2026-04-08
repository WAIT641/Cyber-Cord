using System.Drawing;
using Cyber_Cord.Api.Data;
using Cyber_Cord.Api.Entities;
using Cyber_Cord.Api.Models;
using Cyber_Cord.Api.Repositories;
using Cyber_Cord.Api.Tests.Helpers;
using Microsoft.AspNetCore.Identity;

namespace Cyber_Cord.Api.Tests.Repositories;

public class UsersRepositoryTests : IDisposable
{
    // Testing removal of things is currently not possible, because we are using ExecuteDeleteAsync, which is not supported by InMemoryDatabase
    // Testing of creation and saving of settings also not supported by InMemoryDatabase

    private const int _currentUserId = 1;
    private const string _currentUserEmail = "current@example.org";
    private const int _otherUserId = 2;
    private const string _otherUserEmail = "other@example.org";
    private const int _friendshipId = 1;
    private const int _otherFriendshipId = 2;
    private const int _randomUserId = 3;
    private const string _randomUserEmail = "random@example.org";

    private readonly UserManager<User> _userManager;
    private readonly AppDbContext _context;
    private readonly UsersRepository _repository;

    public UsersRepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        _userManager = TestUserManagerFactory.Create(_context);

        _repository = new(_context, _userManager);
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
        await SeedUserAsync(_currentUserId, _currentUserEmail);

        var user = await _repository.GetUserByIdAsync(_currentUserId);

        Assert.NotNull(user);
        Assert.Equal(
            user,
            await _context.Users.FindAsync(_currentUserId)
            );
    }

    [Fact]
    public async Task GetUserByIdAsync_DoesNotFindUser()
    {
        await SeedUserAsync(_currentUserId, _currentUserEmail);

        var user = await _repository.GetUserByIdAsync(_otherUserId);

        Assert.Null(user);
    }

    [Fact]
    public async Task GetFriendshipByIdAsync_GetsFriendship()
    {
        await SeedUserAsync(_currentUserId, _currentUserEmail);
        await SeedUserAsync(_otherUserId, _otherUserEmail);
        await SeedFriendshipAsync(_friendshipId, _currentUserId, _otherUserId);

        var friendship = await _repository.GetFriendshipByIdAsync(_friendshipId);

        Assert.NotNull(friendship);
        Assert.Equal(
            friendship,
            await _context.Friendships.FindAsync(_friendshipId)
            );
    }

    [Fact]
    public async Task GetFriendshipByIdAsync_Throws()
    {
        await SeedUserAsync(_currentUserId, _currentUserEmail);
        await SeedUserAsync(_otherUserId, _otherUserEmail);
        await SeedFriendshipAsync(_friendshipId, _currentUserId, _otherUserId);

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _repository.GetFriendshipByIdAsync(_otherFriendshipId)
            );
    }

    [Fact]
    public async Task UserHasAccessToFriendshipAsync_IsTrue()
    {
        await SeedUserAsync(_currentUserId, _currentUserEmail);
        await SeedUserAsync(_otherUserId, _otherUserEmail);
        await SeedFriendshipAsync(_friendshipId, _currentUserId, _otherUserId);

        var accessCurrent = await _repository.UserHasAccessToFriendshipAsync(_currentUserId, _friendshipId);
        var accessOther = await _repository.UserHasAccessToFriendshipAsync(_otherUserId, _friendshipId);

        Assert.True(accessCurrent);
        Assert.True(accessOther);
    }

    [Fact]
    public async Task UserHasAccessToFriendshipAsync_Throws()
    {
        await SeedUserAsync(_currentUserId, _currentUserEmail);
        await SeedUserAsync(_otherUserId, _otherUserEmail);
        await SeedFriendshipAsync(_friendshipId, _currentUserId, _otherUserId);

        var randomAccess = await _repository.UserHasAccessToFriendshipAsync(_randomUserId, _friendshipId);

        Assert.False(randomAccess);
    }

    [Fact]
    public async Task UserExistsAsync_ReturnsTrue()
    {
        await SeedUserAsync(_currentUserId, _currentUserEmail);

        var exists = await _repository.UserExistsAsync(_currentUserId);

        Assert.True(exists);
    }

    [Fact]
    public async Task UserExistsAsync_ReturnsFalse()
    {
        await SeedUserAsync(_currentUserId, _currentUserEmail);

        var exists = await _repository.UserExistsAsync(_otherUserId);

        Assert.False(exists);
    }

    [Fact]
    public async Task UsersAreFriendsAsync_ReturnsTrue()
    {
        await SeedUserAsync(_currentUserId, _currentUserEmail);
        await SeedUserAsync(_otherUserId, _otherUserEmail);
        await SeedFriendshipAsync(_friendshipId, _currentUserId, _otherUserId);

        var areFriends = await _repository.UsersAreFriendsAsync(_currentUserId, _otherUserId);

        Assert.True(areFriends);
    }

    [Fact]
    public async Task UsersAreFriendsAsync_ReturnsFalse()
    {
        var areFriends = await _repository.UsersAreFriendsAsync(_currentUserId, _otherUserId);

        Assert.False(areFriends);
    }

    [Fact]
    public async Task GetUserDetailAsync_GetsDetail()
    {
        await SeedUserAsync(_currentUserId, _currentUserEmail);

        var detail = await _repository.GetUserDetailAsync(_currentUserId);

        var user = await _context.Users.FindAsync(_currentUserId);
        Assert.NotNull(detail);
        Assert.Equal(_currentUserId, detail.Id);
        Assert.NotNull(detail.BannerColor);
        Assert.Equal(user!.BannerColor.R, detail.BannerColor.Red);
        Assert.Equal(user!.BannerColor.G, detail.BannerColor.Green);
        Assert.Equal(user!.BannerColor.B, detail.BannerColor.Blue);
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
        await SeedUserAsync(_currentUserId, _currentUserEmail);

        var detail = await _repository.GetUserDetailAsync(_otherUserId);

        Assert.Null(detail);
    }

    [Fact]
    public async Task GetUsersSettingsAsync_ReturnsUsersSettings()
    {
        await SeedUserAsync(_currentUserId, _currentUserEmail);
        await GiveUserSettingsAsync(_currentUserId);

        var settings = await _repository.GetUsersSettingsAsync(_currentUserId);

        Assert.NotNull(settings);
        Assert.Equal(_currentUserId, settings.UserId);
    }

    [Fact]
    public async Task GetPendingRequestsAsync_GetsRequests()
    {
        await SeedUserAsync(_currentUserId, _currentUserEmail);
        await SeedUserAsync(_otherUserId, _otherUserEmail);
        await SeedRequestAsync(_friendshipId, _currentUserId, _otherUserId);

        var currentRequests = await _repository.GetPendingRequestsAsync(_currentUserId);
        var otherRequests = await _repository.GetPendingRequestsAsync(_otherUserId);

        Assert.Single(currentRequests);
        Assert.Single(otherRequests);
        Assert.Equal(_friendshipId, currentRequests[0].Id);
        Assert.Equal(_friendshipId, otherRequests[0].Id);
    }

    [Fact]
    public async Task GetPendingRequestsAsync_FindsNone()
    {
        await SeedUserAsync(_currentUserId, _currentUserEmail);
        await SeedUserAsync(_otherUserId, _otherUserEmail);
        await SeedRequestAsync(_friendshipId, _currentUserId, _otherUserId);

        var randomRequests = await _repository.GetPendingRequestsAsync(_randomUserId);

        Assert.Empty(randomRequests);
    }

    [Fact]
    public async Task SearchMultipleAsync_FindsTwo()
    {
        await SeedUserAsync(_currentUserId, _currentUserEmail);
        await SeedUserAsync(_otherUserId, _otherUserEmail);

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
        await SeedUserAsync(_currentUserId, _currentUserEmail);
        await SeedUserAsync(_otherUserId, _otherUserEmail);

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
        await SeedUserAsync(_currentUserId, _currentUserEmail);
        await SeedUserAsync(_otherUserId, _otherUserEmail);

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
        await SeedUserAsync(_currentUserId, _currentUserEmail);
        await SeedUserAsync(_otherUserId, _otherUserEmail);

        var model = new UserSearchModel
        {
            SearchName = _currentUserEmail,
            SingleResultOnly = true
        };

        var result = await _repository.SearchSingularAsync(model);

        Assert.NotNull(result);
        Assert.Equal(_currentUserId, result.Id);
    }

    [Fact]
    public async Task SearchSingularAsync_FindsNone()
    {
        await SeedUserAsync(_currentUserId, _currentUserEmail);

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
        await SeedUserAsync(_currentUserId, _currentUserEmail);
        await SeedUserAsync(_otherUserId, _otherUserEmail);
        await SeedUserAsync(_randomUserId, _randomUserEmail);
        await SeedFriendshipAsync(_friendshipId, _currentUserId, _otherUserId);
        await SeedFriendshipAsync(_otherFriendshipId, _otherUserId, _currentUserId);

        var model = new FriendSearchModel
        {
            SearchName = "example"
        };

        var results = await _repository.SearchMultipleFriendsAsync(_currentUserId, model);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task SearchMultipleFriendsAsync_FindsOne()
    {
        await SeedUserAsync(_currentUserId, _currentUserEmail);
        await SeedUserAsync(_otherUserId, _otherUserEmail);
        await SeedUserAsync(_randomUserId, _randomUserEmail);
        await SeedFriendshipAsync(_friendshipId, _currentUserId, _otherUserId);
        await SeedFriendshipAsync(_otherFriendshipId, _otherUserId, _currentUserId);

        var model = new FriendSearchModel
        {
            SearchName = "example",
            Limit = 1
        };

        var results = await _repository.SearchMultipleFriendsAsync(_currentUserId, model);

        Assert.Single(results);
    }

    [Fact]
    public async Task SearchMultipleFriendsAsync_FindsNone()
    {
        await SeedUserAsync(_currentUserId, _currentUserEmail);
        await SeedUserAsync(_otherUserId, _otherUserEmail);
        await SeedFriendshipAsync(_friendshipId, _currentUserId, _otherUserId);

        var model = new FriendSearchModel
        {
            SearchName = "something"
        };

        var results = await _repository.SearchMultipleFriendsAsync(_currentUserId, model);

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchSingularFriendsAsync_FindsOne()
    {
        await SeedUserAsync(_currentUserId, _currentUserEmail);
        await SeedUserAsync(_otherUserId, _otherUserEmail);
        await SeedFriendshipAsync(_friendshipId, _currentUserId, _otherUserId);

        var currentResult   = await _repository.SearchSingularFriendsAsync(_currentUserId, new() { SearchUserId = _otherUserId });
        var otherResult     = await _repository.SearchSingularFriendsAsync(_otherUserId, new() { SearchUserId = _currentUserId });

        Assert.NotNull(currentResult.Item1);
        Assert.Null(currentResult.Item2);
        Assert.NotNull(otherResult.Item2);
        Assert.Null(otherResult.Item1);
        Assert.Equal(_friendshipId, currentResult.Item1.Id);
        Assert.Equal(_friendshipId, otherResult.Item2.Id);
    }

    [Fact]
    public async Task SearchSingularFriendsAsync_FindsNone()
    {
        await SeedUserAsync(_currentUserId, _currentUserEmail);
        await SeedUserAsync(_otherUserId, _otherUserEmail);
        await SeedFriendshipAsync(_friendshipId, _currentUserId, _otherUserId);

        var currentResult = await _repository.SearchSingularFriendsAsync(_currentUserId, new() { SearchUserId = _randomUserId });
        var otherResult = await _repository.SearchSingularFriendsAsync(_otherUserId, new() { SearchUserId = _randomUserId });

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
        await SeedUserAsync(_currentUserId, _currentUserEmail, false);
        var user = await _context.Users.FindAsync(_currentUserId);

        await _repository.ValidateUserAsync(user!);

        var result = await _context.Users.FindAsync(_currentUserId);
        Assert.True(result!.IsActivated);
    }

    [Fact]
    public async Task RequestFriendshipAsync_RequestsFriendship()
    {
        await SeedUserAsync(_currentUserId, _currentUserEmail);
        await SeedUserAsync(_otherUserId, _otherUserEmail);

        var friendship = await _repository.RequestFriendshipAsync(_currentUserId, new() { UserId = _otherUserId });

        Assert.NotNull(friendship);
        Assert.NotNull(await _context.Friendships.FindAsync(friendship.Id));
        Assert.Equal(_currentUserId, friendship.RequestingId);
        Assert.Equal(_otherUserId, friendship.ReceivingId);
    }

    [Fact]
    public async Task AcceptFriendshipAsync_AcceptsFriendship()
    {
        await SeedUserAsync(_currentUserId, _currentUserEmail);
        await SeedUserAsync(_otherUserId, _otherUserEmail);
        await SeedRequestAsync(_friendshipId, _currentUserId, _otherUserId);
        var friendRequest = await _context.Friendships.FindAsync(_friendshipId);

        await _repository.AcceptFriendshipAsync(friendRequest!);

        var result = await _context.Friendships.FindAsync(_friendshipId);
        Assert.False(result!.Pending);
    }

    [Fact]
    public async Task UpdateUserAsync_UpdatesUser()
    {
        await SeedUserAsync(_currentUserId, _currentUserEmail);

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

        var user = await _context.Users.FindAsync(_currentUserId);

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
        await SeedUserAsync(_currentUserId, _currentUserEmail);
        var user = await _context.Users.FindAsync(_currentUserId);

        await _repository.ChangeUserPasswordAsync(user!, "HASH");

        var result = await _context.Users.FindAsync(_currentUserId);
        Assert.Equal("HASH", result!.PasswordHash);
    }
}
