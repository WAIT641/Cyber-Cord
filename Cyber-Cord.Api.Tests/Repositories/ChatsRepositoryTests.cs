using System.Drawing;
using Cyber_Cord.Api.Data;
using Cyber_Cord.Api.Entities;
using Cyber_Cord.Api.Models;
using Cyber_Cord.Api.Models.Base;
using Cyber_Cord.Api.Repositories;
using Cyber_Cord.Api.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;

namespace Cyber_Cord.Api.Tests.Repositories;

public class ChatsRepositoryTests : IDisposable
{
    // Note: ExecuteDeleteAsync is not supported by InMemoryDatabase.
    // The following methods are therefore not tested:
    //   RemoveEmptyChatsAsync, RemoveChatUsers, RemoveChatMessages, DeleteChatAsync

    private const int UserId = 1;
    private const int OtherUserId = 2;
    private const int ChatId = 1;
    private const int OtherChatId = 2;
    private const int MessageId = 1;

    private readonly AppDbContext _context;
    private readonly ChatsRepository _repository;

    public ChatsRepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        _repository = new(_context);
    }

    public void Dispose() => _context.Dispose();
    
    private async Task SeedUserAsync(int id)
    {
        _context.Users.Add(new User
        {
            Id = id,
            Email = $"user{id}@example.org",
            UserName = $"user{id}@example.org",
            DisplayName = $"User{id}",
            BannerColor = Color.Black,
            CreatedAt = DateTime.UtcNow,
            Description = ""
        });
        await _context.SaveChangesAsync();
    }

    private async Task<Chat> SeedChatAsync(int id, string? name = "TestChat")
    {
        var chat = new Chat { Id = id, Name = name };
        _context.Chats.Add(chat);
        await _context.SaveChangesAsync();
        return chat;
    }

    private async Task SeedUserChatAsync(int userId, int chatId)
    {
        _context.UserChats.Add(new UserChat { UserId = userId, ChatId = chatId });
        await _context.SaveChangesAsync();
    }

    private async Task<Message> SeedMessageAsync(int id, int userId, int chatId, string content = "Hello")
    {
        var message = new Message
        {
            Id = id,
            UserId = userId,
            ChatId = chatId,
            Content = content,
            CreatedAt = DateTime.UtcNow,
        };
        _context.Messages.Add(message);
        await _context.SaveChangesAsync();
        return message;
    }

    // ── UserIsInChatAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task UserIsInChatAsync_ReturnsTrue_WhenMember()
    {
        await SeedUserAsync(UserId);
        await SeedChatAsync(ChatId);
        await SeedUserChatAsync(UserId, ChatId);

        var result = await _repository.UserIsInChatAsync(UserId, ChatId);

        Assert.True(result);
    }

    [Fact]
    public async Task UserIsInChatAsync_ReturnsFalse_WhenNotMember()
    {
        await SeedUserAsync(UserId);
        await SeedChatAsync(ChatId);

        var result = await _repository.UserIsInChatAsync(UserId, ChatId);

        Assert.False(result);
    }

    // ── UserOwnsMessageAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task UserOwnsMessageAsync_ReturnsTrue_WhenOwner()
    {
        await SeedUserAsync(UserId);
        await SeedChatAsync(ChatId);
        await SeedMessageAsync(MessageId, UserId, ChatId);

        var result = await _repository.UserOwnsMessageAsync(UserId, MessageId);

        Assert.True(result);
    }

    [Fact]
    public async Task UserOwnsMessageAsync_ReturnsFalse_WhenNotOwner()
    {
        await SeedUserAsync(UserId);
        await SeedUserAsync(OtherUserId);
        await SeedChatAsync(ChatId);
        await SeedMessageAsync(MessageId, OtherUserId, ChatId);

        var result = await _repository.UserOwnsMessageAsync(UserId, MessageId);

        Assert.False(result);
    }

    // ── MessageIsInChat ───────────────────────────────────────────────────────

    [Fact]
    public async Task MessageIsInChat_ReturnsTrue_WhenInChat()
    {
        await SeedUserAsync(UserId);
        await SeedChatAsync(ChatId);
        await SeedMessageAsync(MessageId, UserId, ChatId);

        var result = await _repository.MessageIsInChat(ChatId, MessageId);

        Assert.True(result);
    }

    [Fact]
    public async Task MessageIsInChat_ReturnsFalse_WhenNotInChat()
    {
        await SeedUserAsync(UserId);
        await SeedChatAsync(ChatId);
        await SeedChatAsync(OtherChatId);
        await SeedMessageAsync(MessageId, UserId, OtherChatId);

        var result = await _repository.MessageIsInChat(ChatId, MessageId);

        Assert.False(result);
    }

    // ── IsFriendsChatAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task IsFriendsChatAsync_ReturnsTrue_WhenNameIsNull()
    {
        await SeedChatAsync(ChatId, null);

        var result = await _repository.IsFriendsChatAsync(ChatId);

        Assert.True(result);
    }

    [Fact]
    public async Task IsFriendsChatAsync_ReturnsFalse_WhenNameIsSet()
    {
        await SeedChatAsync(ChatId, "GroupChat");

        var result = await _repository.IsFriendsChatAsync(ChatId);

        Assert.False(result);
    }

    // ── SearchChatsAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task SearchChatsAsync_FindsMatchingChats()
    {
        await SeedUserAsync(UserId);
        await SeedChatAsync(ChatId, "Alpha");
        await SeedChatAsync(OtherChatId, "Beta");
        await SeedUserChatAsync(UserId, ChatId);
        await SeedUserChatAsync(UserId, OtherChatId);

        var result = await _repository.SearchChatsAsync(UserId, new ChatSearchModel
        {
            SearchName = "Alpha",
            Limit = 10,
            Order = Ordering.Asc,
        });

        Assert.Single(result);
        Assert.Equal("Alpha", result[0].Name);
    }

    [Fact]
    public async Task SearchChatsAsync_ReturnsAll_WhenSearchNameIsNull()
    {
        await SeedUserAsync(UserId);
        await SeedChatAsync(ChatId, "Alpha");
        await SeedChatAsync(OtherChatId, "Beta");
        await SeedUserChatAsync(UserId, ChatId);
        await SeedUserChatAsync(UserId, OtherChatId);

        var result = await _repository.SearchChatsAsync(UserId, new ChatSearchModel
        {
            SearchName = null,
            Limit = 10,
            Order = Ordering.Asc,
        });

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task SearchChatsAsync_ReturnsEmpty_WhenNoMatch()
    {
        await SeedUserAsync(UserId);
        await SeedChatAsync(ChatId, "Alpha");
        await SeedUserChatAsync(UserId, ChatId);

        var result = await _repository.SearchChatsAsync(UserId, new ChatSearchModel
        {
            SearchName = "xyz",
            Limit = 10,
            Order = Ordering.Asc,
        });

        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchChatsAsync_ReturnsEmpty_WhenUserNotInAnyChat()
    {
        await SeedUserAsync(UserId);
        await SeedChatAsync(ChatId, "Alpha");

        var result = await _repository.SearchChatsAsync(UserId, new ChatSearchModel
        {
            Limit = 10,
            Order = Ordering.Asc,
        });

        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchChatsAsync_RespectsLimit()
    {
        await SeedUserAsync(UserId);
        await SeedChatAsync(ChatId, "Alpha");
        await SeedChatAsync(OtherChatId, "Beta");
        await SeedUserChatAsync(UserId, ChatId);
        await SeedUserChatAsync(UserId, OtherChatId);

        var result = await _repository.SearchChatsAsync(UserId, new ChatSearchModel
        {
            Limit = 1,
            Order = Ordering.Asc,
        });

        Assert.Single(result);
    }

    // ── GetChatByIdAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetChatByIdAsync_ReturnsChat()
    {
        await SeedChatAsync(ChatId, "TestChat");

        var result = await _repository.GetChatByIdAsync(ChatId);

        Assert.NotNull(result);
        Assert.Equal(ChatId, result.Id);
        Assert.Equal("TestChat", result.Name);
    }

    [Fact]
    public async Task GetChatByIdAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _repository.GetChatByIdAsync(ChatId);

        Assert.Null(result);
    }

    // ── GetMessageByIdAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetMessageByIdAsync_ReturnsMessage()
    {
        await SeedUserAsync(UserId);
        await SeedChatAsync(ChatId);
        await SeedMessageAsync(MessageId, UserId, ChatId, "Hi");

        var result = await _repository.GetMessageByIdAsync(MessageId);

        Assert.NotNull(result);
        Assert.Equal(MessageId, result.Id);
        Assert.Equal("Hi", result.Content);
    }

    [Fact]
    public async Task GetMessageByIdAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _repository.GetMessageByIdAsync(MessageId);

        Assert.Null(result);
    }

    // ── GetChatUsersAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetChatUsersAsync_ReturnsUsers()
    {
        await SeedUserAsync(UserId);
        await SeedUserAsync(OtherUserId);
        await SeedChatAsync(ChatId);
        await SeedUserChatAsync(UserId, ChatId);
        await SeedUserChatAsync(OtherUserId, ChatId);

        var result = await _repository.GetChatUsersAsync(ChatId); // TODO: ToReturnModel messing it up I think 

        Assert.Equal(2, result.Count);
        Assert.Contains(result, u => u.Id == UserId);
        Assert.Contains(result, u => u.Id == OtherUserId);
    }

    [Fact]
    public async Task GetChatUsersAsync_ReturnsEmpty_WhenNoMembers()
    {
        await SeedChatAsync(ChatId);

        var result = await _repository.GetChatUsersAsync(ChatId); // TODO: ToReturnModel messing it up I think

        Assert.Empty(result);
    }

    // ── GetChatMessagesAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetChatMessagesAsync_ReturnsMessages()
    {
        await SeedUserAsync(UserId);
        await SeedChatAsync(ChatId);
        await SeedMessageAsync(MessageId, UserId, ChatId);
        await SeedMessageAsync(2, UserId, ChatId);

        var result = await _repository.GetChatMessagesAsync(ChatId, new CursorPaginationFilter());

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Data, m => Assert.Equal(ChatId, m.ChatId));
    }

    [Fact]
    public async Task GetChatMessagesAsync_ReturnsEmpty_WhenNoMessages()
    {
        var result = await _repository.GetChatMessagesAsync(ChatId, new CursorPaginationFilter());

        Assert.Empty(result.Data);
    }

    // ── AddChat / AddUserChat ─────────────────────────────────────────────────

    [Fact]
    public async Task AddChat_AddsChat()
    {
        await SeedUserAsync(UserId);

        var model = new ChatCreateModel { Name = "NewChat" };
        var chat = _repository.AddChat(UserId, model);
        await _context.SaveChangesAsync();

        Assert.NotNull(await _context.Chats.FindAsync(chat.Id));
        Assert.Equal("NewChat", chat.Name);
    }

    [Fact]
    public async Task AddUserChat_AddsUserToChat()
    {
        await SeedUserAsync(UserId);
        var chat = _repository.AddChat(UserId, new ChatCreateModel { Name = "NewChat" });
        await _context.SaveChangesAsync();

        _repository.AddUserChat(UserId, chat);
        await _context.SaveChangesAsync();

        var userChat = await _context.UserChats.FirstOrDefaultAsync(x => x.UserId == UserId && x.ChatId == chat.Id);
        Assert.NotNull(userChat);
    }

    // ── CreateUserChatAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateUserChatAsync_AddsUserToExistingChat()
    {
        await SeedUserAsync(UserId);
        await SeedUserAsync(OtherUserId);
        await SeedChatAsync(ChatId);

        await _repository.CreateUserChatAsync(ChatId, new UserChatCreateModel { UserId = OtherUserId });

        var userChat = await _context.UserChats.FirstOrDefaultAsync(x => x.UserId == OtherUserId && x.ChatId == ChatId);
        Assert.NotNull(userChat);
    }

    // ── PostMessageToChatAsync ────────────────────────────────────────────────

    [Fact]
    public async Task PostMessageToChatAsync_PostsMessage()
    {
        await SeedUserAsync(UserId);
        await SeedChatAsync(ChatId);

        var result = await _repository.PostMessageToChatAsync(
            UserId, ChatId, new MessageCreateModel { Content = "Hello!" });

        Assert.NotNull(result);
        Assert.Equal("Hello!", result.Content);
        Assert.Equal(UserId, result.UserId);
        Assert.NotNull(await _context.Messages.FindAsync(result.Id));
    }

    // ── UpdateChatAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateChatAsync_UpdatesChatName()
    {
        await SeedUserAsync(UserId);
        await SeedChatAsync(ChatId, "OldName");

        var result = await _repository.UpdateChatAsync(ChatId, new ChatUpdateModel { Name = "NewName" });

        Assert.Equal("NewName", result.Name);
        var persisted = await _context.Chats.FindAsync(ChatId);
        Assert.Equal("NewName", persisted!.Name);
    }

    // ── UpdateMessageAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateMessageAsync_UpdatesContent()
    {
        await SeedUserAsync(UserId);
        await SeedChatAsync(ChatId);
        await SeedMessageAsync(MessageId, UserId, ChatId, "Original");

        var result = await _repository.UpdateMessageAsync(MessageId, new MessageUpdateModel { Content = "Edited" });

        Assert.Equal("Edited", result.Content);
        var persisted = await _context.Messages.FindAsync(MessageId);
        Assert.Equal("Edited", persisted!.Content);
    }

    // ── RemoveUserFromChatAsync ───────────────────────────────────────────────

    [Fact]
    public async Task RemoveUserFromChatAsync_RemovesUser()
    {
        await SeedUserAsync(UserId);
        await SeedChatAsync(ChatId);
        await SeedUserChatAsync(UserId, ChatId);

        await _repository.RemoveUserFromChatAsync(ChatId, UserId);

        var userChat = await _context.UserChats.FirstOrDefaultAsync(x => x.UserId == UserId && x.ChatId == ChatId);
        Assert.Null(userChat);
    }

    // ── DeleteMessageFromChatAsync ────────────────────────────────────────────

    [Fact]
    public async Task DeleteMessageFromChatAsync_DeletesMessage()
    {
        await SeedUserAsync(UserId);
        await SeedChatAsync(ChatId);
        await SeedMessageAsync(MessageId, UserId, ChatId);

        await _repository.DeleteMessageFromChatAsync(MessageId);

        Assert.Null(await _context.Messages.FindAsync(MessageId));
    }
}
