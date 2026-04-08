using Cyber_Cord.Api.Entities;
using Cyber_Cord.Api.Models;

namespace Cyber_Cord.Api.Repositories;

public interface IUsersRepository
{
    Task<User?> GetUserByIdAsync(int id);
    Task<Friendship> GetFriendshipByIdAsync(int id);
    Task<bool> UserHasAccessToFriendshipAsync(int userId, int friendshipId);
    Task<bool> UserExistsAsync(int userId);
    Task<bool> UsersAreFriendsAsync(int firstUser, int secondUser);
    Task<UserDetailModel?> GetUserDetailAsync(int userId);
    Task<Settings?> GetUsersSettingsAsync(int userId);
    Task<List<FriendRequestDetailModel>> GetPendingRequestsAsync(int userId);
    Task<ChatReturnModel?> GetFriendsChatAsync(int friendshipId);
    Task<Friendship?> GetPendingRequestAsync(int userId, int friendshipId);
    Task<List<UserShortReturnModel>> SearchMultipleAsync(UserSearchModel model);
    Task<UserShortReturnModel?> SearchSingularAsync(UserSearchModel model);
    Task<List<FriendReturnModel>> SearchMultipleFriendsAsync(int userId, FriendSearchModel model);

    /// <returns>(requested, received)</returns>
    Task<(FriendReturnModel?, FriendReturnModel?)> SearchSingularFriendsAsync(int userId, FriendSearchModel model);

    Task<User> CreateUserAsync(UserCreateModel model, string passwordHash);
    Task CreateSettingsForUserAsync(int userId);
    Task SaveUsersSettingsAsync(Settings settings);
    Task ValidateUserAsync(User user);
    Task<Friendship> RequestFriendshipAsync(int userId, FriendRequestCreateModel model);
    Task AcceptFriendshipAsync(Friendship friendRequest);
    Task<UserDetailModel> UpdateUserAsync(User user, UserUpdateModel model);
    Task ChangeUserPasswordAsync(User user, string passwordHash);
    Task RemoveRequestedFriendshipsAsync(int userId);
    Task RemoveReceivedFriendshipsAsync(int userId);
    Task RemoveUsersUserChatsAsync(int userId);
    Task RemoveUsersSettingsAsync(int userId);
    Task RemoveUsersMessagesAsync(int userId);
    Task RemoveUsersUserServers(int userId);
    Task DeleteUserAsync(int userId);
    Task RemoveFriendshipAsync(int friendshipId);

    Task AssignRolesToUserAsync(User user, params string[] roles);
}