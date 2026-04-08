using Cyber_Cord.Api.Entities;
using Cyber_Cord.Api.Models;
using Cyber_Cord.Api.Repositories;

namespace Cyber_Cord.Api.Tests.Stubs;

public class StubUserRepository : IUsersRepository
{
    public readonly User User1 = new()
    {
        DisplayName = "User1",
        IsActivated = true,
        UserName = "User1",
        Id = 1,
    };
    
    public readonly User User2 = new()
    {
        DisplayName = "User2",
        IsActivated = true,
        UserName = "User2",
        Id = 2
    };

    public Task<User?> GetUserByIdAsync(int id) => Task.FromResult(User1)!;

    public Task<Friendship> GetFriendshipByIdAsync(int id) => throw new NotImplementedException();

    public Task<bool> UserHasAccessToFriendshipAsync(int userId, int friendshipId) => throw new NotImplementedException();

    public Task<bool> UserExistsAsync(int userId) => throw new NotImplementedException();

    public Task<bool> UsersAreFriendsAsync(int firstUser, int secondUser) => throw new NotImplementedException();

    public Task<UserDetailModel?> GetUserDetailAsync(int userId) => throw new NotImplementedException();
    public Task<Settings?> GetUsersSettingsAsync(int userId) => throw new NotImplementedException();

    public Task<List<FriendRequestDetailModel>> GetPendingRequestsAsync(int userId) => throw new NotImplementedException();

    public Task<ChatReturnModel?> GetFriendsChatAsync(int friendshipId) => throw new NotImplementedException();

    public Task<Friendship?> GetPendingRequestAsync(int userId, int friendshipId) => throw new NotImplementedException();

    public Task<List<UserShortReturnModel>> SearchMultipleAsync(UserSearchModel model) => throw new NotImplementedException();

    public Task<UserShortReturnModel?> SearchSingularAsync(UserSearchModel model) => throw new NotImplementedException();

    public Task<List<FriendReturnModel>> SearchMultipleFriendsAsync(int userId, FriendSearchModel model) => throw new NotImplementedException();

    public Task<(FriendReturnModel?, FriendReturnModel?)> SearchSingularFriendsAsync(int userId, FriendSearchModel model) => throw new NotImplementedException();

    public Task<User> CreateUserAsync(UserCreateModel model, string passwordHash) => throw new NotImplementedException();

    public Task CreateSettingsForUserAsync(int userId) => throw new NotImplementedException();

    public Task SaveUsersSettingsAsync(Settings settings) => throw new NotImplementedException();

    public Task ValidateUserAsync(User user) => throw new NotImplementedException();

    public Task<Friendship> RequestFriendshipAsync(int userId, FriendRequestCreateModel model) => throw new NotImplementedException();

    public Task AcceptFriendshipAsync(Friendship friendRequest) => throw new NotImplementedException();

    public Task<UserDetailModel> UpdateUserAsync(User user, UserUpdateModel model) => throw new NotImplementedException();

    public Task ChangeUserPasswordAsync(User user, string passwordHash) => throw new NotImplementedException();

    public Task RemoveRequestedFriendshipsAsync(int userId) => throw new NotImplementedException();
    public Task RemoveReceivedFriendshipsAsync(int userId) => throw new NotImplementedException();

    public Task RemoveUsersUserChatsAsync(int userId) => throw new NotImplementedException();

    public Task RemoveUsersSettingsAsync(int userId) => throw new NotImplementedException();

    public Task RemoveUsersMessagesAsync(int userId) => throw new NotImplementedException();

    public Task RemoveUsersUserServers(int userId) => throw new NotImplementedException();

    public Task DeleteUserAsync(int userId) => throw new NotImplementedException();

    public Task RemoveFriendshipAsync(int friendshipId) => throw new NotImplementedException();
    public Task AssignRolesToUserAsync(User user, params string[] roles) => throw new NotImplementedException();
}