using Cyber_Cord.Api.Data;
using Cyber_Cord.Api.Entities;
using Cyber_Cord.Api.Extensions;
using Cyber_Cord.Api.Models;
using Cyber_Cord.Api.Models.Base;
using Microsoft.EntityFrameworkCore;

namespace Cyber_Cord.Api.Repositories;

public class ChatsRepository(AppDbContext context) : IChatsRepository
{
    public async Task<bool> UserIsInChatAsync(int userId, int chatId)
    {
        
        var result = await context.Chats
            .Include(c => c.UserChats)
            .AnyAsync(c => c.Id == chatId && c.UserChats.Any(uc => uc.UserId == userId));

        return result;
    }

    public async Task<bool> UserOwnsMessageAsync(int userId, int messageId)
    {
        var result = await context.Messages.AnyAsync(x => x.Id == messageId && x.UserId == userId);

        return result;
    }

    public async Task<bool> MessageIsInChat(int chatId, int messageId)
    {
        var result = await context.Messages.AnyAsync(x => x.Id == messageId && x.ChatId == chatId);

        return result;
    }

    public async Task<bool> IsFriendsChatAsync(int chatId)
    {
        return await context.Chats.AnyAsync(c => c.Id == chatId && c.Name == null);
    }

    public async Task<List<ChatReturnModel>> SearchChatsAsync(int userId, ChatSearchModel model)
    {
        var result = await context.Chats
            .AsNoTracking()
            .Include(x => x.UserChats)
            .Where(x => x.UserChats.Any(uc => uc.UserId == userId))
            .Where(x => model.SearchName == null || (x.Name != null && x.Name.Contains(model.SearchName)))
            .OrderBy(model.Order!.Value, x => x.Name)
            .Take(model.Limit)
            .Select(x => new ChatReturnModel
            {
                Id = x.Id,
                Name = x.Name,
            })
            .ToListAsync();

        return result;
    }

    public async Task<ChatReturnModel?> GetChatByIdAsync(int chatId)
    {
        var result = await context.Chats
            .Select(x => new ChatReturnModel
            {
                Id = x.Id,
                Name = x.Name
            })
            .FirstOrDefaultAsync(x => x.Id == chatId);

        return result;
    }

    public async Task<Message?> GetMessageByIdAsync(int messageId)
    {
        var message = await context.Messages.FirstOrDefaultAsync(x => x.Id == messageId);

        return message;
    }

    public async Task<List<UserReturnModel>> GetChatUsersAsync(int chatId)
    {
        var users = await context.Chats
            .Include(x => x.UserChats)
            .ThenInclude(uc => uc.User)
            .Where(x => x.Id == chatId)
            .SelectMany(x => x.UserChats.Select(uc => uc.User.ToReturnModel()))
            .ToListAsync();

        return users;
    }

    public async Task<CursorPaginatedResult<MessageReturnModel>> GetChatMessagesAsync(int chatId, CursorPaginationFilter filter)
    {
        var messages = await context.Messages
            .Where(x => x.ChatId == chatId)
            .Select(m => new MessageReturnModel
            {
                Id = m.Id,
                ChatId = m.ChatId,
                UserId = m.UserId,
                Content = m.Content,
                CreatedAt = m.CreatedAt
            })
            .ToCursorPaginatedAsync(filter);

        return messages;
    }

    public Chat AddChat(int userId, ChatCreateModel model)
    {
        var chat = new Chat
        {
            Name = model.Name
        };

        context.Chats.Add(chat);

        return chat;
    }

    public void AddUserChat(int userId, Chat chat)
    {
        var userChat = new UserChat
        {
            Chat = chat,
            UserId = userId
        };

        context.UserChats.Add(userChat);
    }

    public async Task CreateUserChatAsync(int chatId, UserChatCreateModel model)
    {
        var userChat = new UserChat
        {
            ChatId = chatId,
            UserId = model.UserId!.Value
        };

        context.UserChats.Add(userChat);

        await context.SaveChangesAsync();
    }

    public async Task<MessageReturnModel> PostMessageToChatAsync(int userId, int chatId, MessageCreateModel model)
    {
        var message = new Message
        {
            Content = model.Content,
            UserId = userId,
            ChatId = chatId,
            CreatedAt = DateTime.UtcNow,
        };

        context.Messages.Add(message);

        await context.SaveChangesAsync();

        var returnModel = new MessageReturnModel
        {
            Id = message.Id,
            UserId = message.UserId,
            Content = message.Content,
            CreatedAt = message.CreatedAt
        };

        return returnModel;
    }

    public async Task<ChatReturnModel> UpdateChatAsync(int chatId, ChatUpdateModel model)
    {
        var chat = await context.Chats
            .Include(x => x.UserChats)
            .FirstAsync(x => x.Id == chatId);

        chat.Name = model.Name;

        await context.SaveChangesAsync();

        var returnModel = new ChatReturnModel
        {
            Id = chat.Id,
            Name = chat.Name,
        };

        return returnModel;
    }

    public async Task<MessageReturnModel> UpdateMessageAsync(int messageId, MessageUpdateModel model)
    {
        var message = await context.Messages.FirstAsync(x => x.Id == messageId);

        message.Content = model.Content;

        await context.SaveChangesAsync();

        var returnModel = new MessageReturnModel
        {
            Id = message.Id,
            UserId = message.UserId,
            Content = message.Content,
            CreatedAt = message.CreatedAt
        };

        return returnModel;
    }

    public async Task RemoveEmptyChatsAsync()
    {
        await context.Chats
            .Include(x => x.UserChats)
            .Where(x => x.UserChats.Count == 0)
            .ExecuteDeleteAsync();
    }

    public async Task RemoveChatUsers(int chatId)
    {
        await context.UserChats
            .Where(x => x.ChatId == chatId)
            .ExecuteDeleteAsync();
    }

    public async Task RemoveChatMessages(int chatId)
    {
        await context.Messages
            .Where(x => x.ChatId == chatId)
            .ExecuteDeleteAsync();
    }

    public async Task DeleteChatAsync(int chatId)
    {
        await context.Chats
            .Where(x => x.Id == chatId)
            .ExecuteDeleteAsync();
    }

    public async Task RemoveUserFromChatAsync(int chatId, int userId)
    {
        var userChat = await context.UserChats.FirstAsync(x => x.UserId == userId && x.ChatId == chatId);

        context.UserChats.Remove(userChat);

        await context.SaveChangesAsync();
    }

    public async Task DeleteMessageFromChatAsync(int messageId)
    {
        var message = await context.Messages.FirstAsync(x => x.Id == messageId);

        context.Messages.Remove(message);

        await context.SaveChangesAsync();
    }
}