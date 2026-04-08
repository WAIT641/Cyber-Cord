using Cyber_Cord.Api.Models;

namespace Cyber_Cord.Api.Jobs;

public interface IUserJobs
{
    Task CheckActivatedUser(int userId);
    Task CheckActivatedUserSendEmail(int userId);
    Task NotifyAllUsersInUsersChats(List<ChatReturnModel> chats);
    void NotifyUsersFriends(List<FriendReturnModel> friends);
    void NotifyUsersPendingRequests(int currentUserId, List<FriendRequestDetailModel> requests);
    Task NotifyUsersServers(List<int> servers);
}