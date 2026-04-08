using Cyber_Cord.Api.Data;
using Cyber_Cord.Api.Models;
using Cyber_Cord.Api.Repositories;
using Cyber_Cord.Api.Services;
using Shared.Models;

namespace Cyber_Cord.Api.Jobs;

public class UserJobs(
    AppDbContext context,
    ICustomEmailSender emailSender,
    IWebSocketHandler wsHandler,
    IChatsRepository chatsRepository,
    IServersRepository serversRepository
) : IUserJobs {
    public async Task CheckActivatedUser(int userId)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();

        var user = context.Users.FirstOrDefault(u => u.Id == userId);

        if (user is null || user.IsActivated)
        {
            await transaction.CommitAsync();
            return;
        }

        context.Users.Remove(user);
        await context.SaveChangesAsync();

        await transaction.CommitAsync();
    }
    
    public async Task CheckActivatedUserSendEmail(int userId)
    {
        var user = context.Users.FirstOrDefault(u => u.Id == userId);

        if (user is null || user.IsActivated)
        {
            return;
        }
        
        await emailSender.SendEmailAsync(user.Email!, "Account not activated", emailSender.GetNoticeEmailMessage(user.UserName!));
    }

    public async Task NotifyAllUsersInUsersChats(List<ChatReturnModel> chats)
    {
        foreach (var chat in chats)
        {
            await SendMessageToAllUsersInChatAsync(chat.Id);
        }
    }

    public void NotifyUsersFriends(List<FriendReturnModel> friends)
    {
        foreach (var friendship in friends)
        {
            var message = new WebSocketGeneralMessageModel
            {
                Scope = WebSocketGeneralMessageModel.ScopeType.Friend
            };

            wsHandler.SendToUser(friendship.OtherUser.Id, message);
        }
    }

    public void NotifyUsersPendingRequests(int currentUserId, List<FriendRequestDetailModel> requests)
    {
        foreach (var request in requests)
        {
            var message = new WebSocketGeneralMessageModel
            {
                Scope = WebSocketGeneralMessageModel.ScopeType.Request
            };

            wsHandler.SendToUser(currentUserId == request.ReceivingUser.Id
                ? request.RequestingUser.Id
                : request.ReceivingUser.Id
                , message
                );
        }
    }

    public async Task NotifyUsersServers(List<int> servers)
    {
        foreach (var server in servers)
        {
            await SendMessageToAllUsersInServerAsync(server);
        }
    }

    private async Task SendMessageToAllUsersInChatAsync(int chatId)
    {
        var users = await chatsRepository.GetChatUsersAsync(chatId);

        var message = new WebSocketGeneralMessageModel
        {
            Scope = WebSocketGeneralMessageModel.ScopeType.Chat
        };

        foreach (var user in users)
        {
            wsHandler.SendToUser(user.Id, message);
        }
    }

    private async Task SendMessageToAllUsersInServerAsync(int serverId)
    {
        var users = await serversRepository.GetServerMemberIdsAsync(serverId);

        var message = new WebSocketServerModel
        {
            Scope = WebSocketServerModel.ScopeType.User,
            ServerId = serverId
        };

        foreach (var user in users)
        {
            wsHandler.SendToUser(user, message);
        }
    }
}
