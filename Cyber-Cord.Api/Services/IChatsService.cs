using Cyber_Cord.Api.Models;
using Cyber_Cord.Api.Models.Base;
using Shared.Models.ApiModels;

namespace Cyber_Cord.Api.Services;

public interface IChatsService
{
    Task<List<ChatReturnModel>> SearchChatsAsync(ChatSearchModel model);
    Task<ChatReturnModel> GetChatByIdAsync(int chatId);
    Task<List<UserReturnModel>> GetChatUsersAsync(int chatId);
    Task<CursorPaginatedResult<MessageReturnModel>> GetChatMessagesAsync(int chatId, CursorPaginationFilter filter);
    Task<VoiceTokenDto> GetChatVoiceTokenAsync(int chatId);
    Task<ChatReturnModel> CreateChatAsync(ChatCreateModel model);
    Task AddUserToChatAsync(int chatId, UserChatCreateModel model, bool allowFriendChats = false);
    Task<MessageReturnModel> PostMessageToChatAsync(int chatId, MessageCreateModel model);
    Task<ChatReturnModel> UpdateChatAsync(int chatId, ChatUpdateModel model);
    Task<MessageReturnModel> UpdateMessageAsync(int chatId, int messageId, MessageUpdateModel model);
    Task DeleteChatAsync(int chatId);
    Task RemoveUserFromChatAsync(int chatId, int userId);
    Task DeleteMessageFromChatAsync(int chatId, int messageId);
}