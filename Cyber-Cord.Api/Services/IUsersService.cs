using Cyber_Cord.Api.Entities;
using Cyber_Cord.Api.Models;
using Microsoft.AspNetCore.JsonPatch;

namespace Cyber_Cord.Api.Services;

public interface IUsersService
{
    Task CheckUserPasswordAsync(int? userId, string? password, bool enforceActivated = true);
    Task EnsureUsersAreFriendsAsync(int userId);
    Task<UserReturnModel> GetUserByIdAsync(int userId);
    Task<UserReturnModel> GetCurrentUserAsync();
    Task<List<UserShortReturnModel>> SearchUsersAsync(UserSearchModel model);
    Task<UserDetailModel> GetUserDetailAsync(int userId);
    Task<SettingsReturnModel> GetUsersSettingsAsync(int userId);
    Task<List<FriendReturnModel>> SearchFriendsAsync(int userId, FriendSearchModel model);
    Task<List<FriendRequestDetailModel>> GetPendingRequestsAsync(int userId);
    Task<ChatReturnModel> GetFriendsChatAsync(int userId, int friendshipId);
    Task<UserReturnModel> CreateUserAsync(UserCreateModel model);
    Task ValidateUserAsync(int userId, string validationToken);
    Task ResendValidationCodeAsync(int userId, string password);
    Task RequestFriendshipAsync(int userId, FriendRequestCreateModel model);
    Task<FriendRequestDetailModel> AcceptFriendshipAsync(int userId, int friendshipId);
    Task<UserDetailModel> UpdateUserAsync(int userId, UserUpdateModel model);
    Task ChangeUserPasswordAsync(int userId, UserPasswordChangeModel model);
    Task<SettingsReturnModel> UpdateUsersSettingsAsync(int userId, JsonPatchDocument<Settings> document);
    Task DeleteUserAsync(int userId, string password);
    Task RemoveFriendshipAsync(int userId, int friendshipId);
    Task PingAsync(int id);
    Task AssignRolesToUserAsync(int userId, RolesAssignmentModel model);
}