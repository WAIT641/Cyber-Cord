using Cyber_Cord.Api.Exceptions;
using Cyber_Cord.Api.Models;
using Cyber_Cord.Api.Models.Base;
using Cyber_Cord.Api.Repositories;
using Cyber_Cord.Api.Types.Interfaces;
using Shared.Models;
using Shared.Models.ApiModels;
using Shared.Types.Interfaces;

namespace Cyber_Cord.Api.Services;

public class ChatsService(
    IChatsRepository repository,
    IUsersRepository usersRepository,
    IWebSocketHandler handler,
    ICurrentUserContext userContext,
    IUnitOfWork uow,
    ICallService callService
) : IChatsService
{
    private int UserId => userContext.GetId();

    public async Task<List<ChatReturnModel>> SearchChatsAsync(ChatSearchModel model)
    {
        return await repository.SearchChatsAsync(UserId, model);
    }

    public async Task<ChatReturnModel> GetChatByIdAsync(int chatId)
    {
        await CheckAccessToChatAsync(chatId);

        var result = await repository.GetChatByIdAsync(chatId);

        if (result is null)
            throw new NotFoundException($"Could not find chat with id {chatId}");

        return result;
    }

    public async Task<List<UserReturnModel>> GetChatUsersAsync(int chatId)
    {
        await CheckAccessToChatAsync(chatId);

        return await repository.GetChatUsersAsync(chatId);
    }

    public async Task<CursorPaginatedResult<MessageReturnModel>> GetChatMessagesAsync(int chatId, CursorPaginationFilter filter)
    {
        await CheckAccessToChatAsync(chatId);

        return await repository.GetChatMessagesAsync(chatId, filter);
    }

    public async Task<ChatReturnModel> CreateChatAsync(ChatCreateModel model)
    {
        var chat = repository.AddChat(UserId, model);
        repository.AddUserChat(UserId, chat);

        await uow.SaveChangesAsync();

        NotifyUserAboutChatPresence(UserId);

        var returnModel = new ChatReturnModel
        {
            Id = chat.Id,
            Name = chat.Name,
        };

        return returnModel;
    }

    public async Task AddUserToChatAsync(int chatId, UserChatCreateModel model, bool allowFriendChats = false)
    {
        await CheckAccessToChatAsync(chatId);

        // Confirm existence of the user
        await usersRepository.GetUserByIdAsync(UserId);

        if (await repository.UserIsInChatAsync(model.UserId!.Value, chatId))
        {
            throw new AlreadyExistsException($"User {userContext.GetName()} is already in chat with id {chatId}");
        }

        if (await repository.IsFriendsChatAsync(chatId) && !allowFriendChats)
        {
            throw new BadRequestException("Adding users is not allowed in a friends chat");
        }

        await repository.CreateUserChatAsync(chatId, model);

        await NotifyUsersAboutChangedUserPresenceAsync(chatId);
    }

    public async Task<MessageReturnModel> PostMessageToChatAsync(int chatId, MessageCreateModel model)
    {
        await CheckAccessToChatAsync(chatId);

        var result = await repository.PostMessageToChatAsync(UserId, chatId, model);

        await NotifyChatMembersAboutMessageAsync(chatId, result.Id, WebSocketMessageActionModel.ActionType.Received);

        return result;
    }

    public async Task<ChatReturnModel> UpdateChatAsync(int chatId, ChatUpdateModel model)
    {
        await CheckAccessToChatAsync(chatId);

        if (await repository.IsFriendsChatAsync(chatId))
        {
            throw new BadRequestException("Cannot modify friend chats");
        }

        var result = await repository.UpdateChatAsync(chatId, model);

        await NotifyUsersAboutChatUpdateAsync(chatId);

        return result;
    }

    public async Task<MessageReturnModel> UpdateMessageAsync(int chatId, int messageId, MessageUpdateModel model)
    {
        await CheckAccessToChatAsync(chatId);

        await CheckAccessToMessageAsync(chatId, messageId);

        var result = await repository.UpdateMessageAsync(messageId, model);

        await NotifyChatMembersAboutMessageAsync(chatId, messageId, WebSocketMessageActionModel.ActionType.Altered);

        return result;
    }

    public async Task DeleteChatAsync(int chatId)
    {
        await CheckAccessToChatAsync(chatId);

        if (await repository.IsFriendsChatAsync(chatId))
        {
            throw new BadRequestException("Cannot delete friend chats");
        }

        var query = await QueryUsersAboutRemovalAsync(chatId);

        await uow.ExecuteInTransactionAsync(async Task (transaction) =>
        {
            await repository.RemoveChatUsers(chatId);

            await repository.RemoveChatMessages(chatId);

            await repository.DeleteChatAsync(chatId);

            await transaction.CommitAsync();
        });

        query();
    }

    public async Task RemoveUserFromChatAsync(int chatId, int userId)
    {
        await CheckAccessToChatAsync(chatId);

        if (await repository.IsFriendsChatAsync(chatId))
        {
            throw new BadRequestException("Cannot remove friends from a friend chat");
        }

        var userIsInChat = await repository.UserIsInChatAsync(userId, chatId);

        if (!userIsInChat)
        {
            throw new NotFoundException($"The user with id {userId} is not in chat with id {chatId}");
        }

        var userCount = (await repository.GetChatUsersAsync(chatId)).Count;

        if (userCount == 1)
        {
            await DeleteChatAsync(chatId);
            return;
        }

        var query = await QueryUsersAboutChangedUserPresenceAsync(chatId);

        await repository.RemoveUserFromChatAsync(chatId, userId);

        query();
    }

    public async Task DeleteMessageFromChatAsync(int chatId, int messageId)
    {
        await CheckAccessToChatAsync(chatId);

        await CheckAccessToMessageAsync(chatId, messageId);

        await repository.DeleteMessageFromChatAsync(messageId);

        await NotifyChatMembersAboutMessageAsync(chatId, messageId, WebSocketMessageActionModel.ActionType.Removed);
    }

    public async Task HandleCall(int chatId, CallMessageModel model)
    {
        await CheckAccessToChatAsync(chatId);
        
        // TODO: for now just one on one, add support for multiple connections
        var users = await GetChatUsersAsync(chatId);

        if (users.Count == 2)
        {
            var otherUser = users.Where(x => x.Id != UserId).FirstOrDefault();
            if (otherUser is null)
                throw new BadRequestException("Cannot start call without other person");
            
            callService.HandleP2PCall(model.OriginatingUserId, otherUser.Id, model);
        }
    }

    // === End of public API ===

    private async Task CheckAccessToChatAsync(int chatId)
    {
        var chat = await repository.GetChatByIdAsync(chatId);

        if (chat is null)
        {
            throw new NotFoundException($"Chat with id {chatId} could not be found");
        }

        var userHasAccess = await repository.UserIsInChatAsync(UserId, chatId);

        if (!userHasAccess)
        {
            throw new ForbiddenException($"User with {userContext.GetName()} cannot access the chat with id {chatId}");
        }
    }

    private async Task CheckAccessToMessageAsync(int chatId, int messageId)
    {
        var messageExists = await repository.GetMessageByIdAsync(messageId) is not null;

        if (!messageExists)
        {
            throw new NotFoundException($"Message with id {messageId} could not be found");
        }

        var userOwnsMessage = await repository.UserOwnsMessageAsync(UserId, messageId);

        if (!userOwnsMessage)
        {
            throw new ForbiddenException($"User {userContext.GetName()} cannot modify the message with id {messageId}");
        }

        var messageIsInChat = await repository.MessageIsInChat(chatId, messageId);

        if (!messageIsInChat)
        {
            throw new BadRequestException($"Message with id {messageId} does not belong to the chat with id {chatId}");
        }
    }

    private void NotifyUserAboutChatPresence(int userId)
    {
        var message = new WebSocketGeneralMessageModel
        {
            Scope = WebSocketGeneralMessageModel.ScopeType.Chat
        };

        handler.SendToUser(userId, message);
    }

    private async Task NotifyChatMembersAboutMessageAsync(int chatId, int messageId, WebSocketMessageActionModel.ActionType actionType)
    {
        var message = new WebSocketMessageActionModel
        {
            ChatId = chatId,
            Action = actionType,
            MessageId = messageId
        };

        var users = await repository.GetChatUsersAsync(chatId);

        foreach (var user in users)
        {
            handler.SendToUser(user.Id, message);
        }
    }

    private async Task<Action> QueryUsersToNotifyAboutChangeAsync(int chatId, IWebSocketMessage message)
    {
        var users = await GetChatUsersAsync(chatId);

        return () =>
        {
            foreach (var user in users)
            {
                handler.SendToUser(user.Id, message);
            }
        };
    }

    private async Task NotifyUsersAboutChangeAsync(int chatId, IWebSocketMessage message)
    {
        var action = await QueryUsersToNotifyAboutChangeAsync(chatId, message);

        action();
    }

    private async Task NotifyUsersAboutChatUpdateAsync(int serverId)
    {
        var message = new WebSocketGeneralMessageModel
        {
            Scope = WebSocketGeneralMessageModel.ScopeType.Chat
        };

        await NotifyUsersAboutChangeAsync(serverId, message);
    }

    private async Task<Action> QueryUsersAboutRemovalAsync(int chatId)
    {
        var message = new WebSocketGeneralMessageModel
        {
            Scope = WebSocketGeneralMessageModel.ScopeType.Chat
        };

        return await QueryUsersToNotifyAboutChangeAsync(chatId, message);
    }

    private async Task<Action> QueryUsersAboutChangedUserPresenceAsync(int chatId)
    {
        var message = new WebSocketGeneralMessageModel
        {
            Scope = WebSocketGeneralMessageModel.ScopeType.Chat
        };

        return await QueryUsersToNotifyAboutChangeAsync(chatId, message);
    }

    private async Task NotifyUsersAboutChangedUserPresenceAsync(int chatId)
    {
        var action = await QueryUsersAboutChangedUserPresenceAsync(chatId);
        action();
    }
}
