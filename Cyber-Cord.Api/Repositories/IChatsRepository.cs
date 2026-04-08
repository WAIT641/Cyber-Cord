using Cyber_Cord.Api.Entities;
using Cyber_Cord.Api.Models;
using Cyber_Cord.Api.Models.Base;

namespace Cyber_Cord.Api.Repositories;

public interface IChatsRepository
{
    Task<bool> UserIsInChatAsync(int userId, int chatId);
    Task<bool> UserOwnsMessageAsync(int userId, int messageId);
    Task<bool> MessageIsInChat(int chatId, int messageId);
    Task<bool> IsFriendsChatAsync(int chatId);
    Task<List<ChatReturnModel>> SearchChatsAsync(int userId, ChatSearchModel model);
    Task<ChatReturnModel?> GetChatByIdAsync(int chatId);
    Task<Message?> GetMessageByIdAsync(int messageId);
    Task<List<UserReturnModel>> GetChatUsersAsync(int chatId);
    Task<CursorPaginatedResult<MessageReturnModel>> GetChatMessagesAsync(int chatId, CursorPaginationFilter filter);
    Chat AddChat(int userId, ChatCreateModel model);
    void AddUserChat(int userId, Chat chat);
    Task CreateUserChatAsync(int chatId, UserChatCreateModel model);
    Task<MessageReturnModel> PostMessageToChatAsync(int userId, int chatId, MessageCreateModel model);
    Task<ChatReturnModel> UpdateChatAsync(int chatId, ChatUpdateModel model);
    Task<MessageReturnModel> UpdateMessageAsync(int messageId, MessageUpdateModel model);
    Task RemoveEmptyChatsAsync();
    Task RemoveChatUsers(int chatId);
    Task RemoveChatMessages(int chatId);
    Task DeleteChatAsync(int chatId);
    Task RemoveUserFromChatAsync(int chatId, int userId);
    Task DeleteMessageFromChatAsync(int messageId);
}