using System.Drawing;
using Cyber_Cord.Api.Data;
using Cyber_Cord.Api.Entities;
using Cyber_Cord.Api.Exceptions;
using Cyber_Cord.Api.Models.Base;
using Cyber_Cord.Api.Repositories;
using Cyber_Cord.Api.Tests.Helpers;
using Shared.Enums;

namespace Cyber_Cord.Api.Tests.Repositories;

public class ServersRepositoryTests : IDisposable
{
    // Note: ExecuteDeleteAsync is not supported by InMemoryDatabase.
    // The following methods are therefore not tested:
    //   DeleteServerAsync, DeleteUserServersForServerAsync, DeleteBansForServerAsync,
    //   DeleteChannelAsync, DeleteChannelsForServerAsync,
    //   DeleteMessagesForChannelAsync, DeleteMessagesForServerAsync

    private const int OwnerId = 1;
    private const int MemberId = 2;
    private const int ServerId = 1;
    private const int OtherServerId = 2;
    private const int ChannelId = 1;
    private const int MessageId = 1;

    private readonly AppDbContext _context;
    private readonly ServersRepository _repository;

    public ServersRepositoryTests()
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
            CreatedAt = DateTime.MinValue,
            Description = $"Description for user {id}"
        });
        await _context.SaveChangesAsync();
    }

    private async Task SeedServerAsync(int id, int ownerId, string name = "TestServer")
    {
        _context.Servers.Add(new Server
        {
            Id = id,
            Name = name,
            Description = $"Description of {name}",
            OwnerId = ownerId,
        });
        await _context.SaveChangesAsync();
    }

    private async Task SeedUserServerAsync(int userId, int serverId)
    {
        _context.UserServers.Add(new UserServer { UserId = userId, ServerId = serverId });
        await _context.SaveChangesAsync();
    }

    private async Task SeedBanAsync(int userId, int serverId)
    {
        _context.BanUserServers.Add(new BanUserServer { UserId = userId, ServerId = serverId });
        await _context.SaveChangesAsync();
    }

    private async Task<Channel> SeedChannelAsync(int id, int serverId, string name = "General")
    {
        var channel = new Channel
        {
            Id = id, 
            Name = name, 
            ServerId = serverId,
            Description = $"Description for channel {id}"
        };
        _context.Channels.Add(channel);
        await _context.SaveChangesAsync();
        return channel;
    }

    private async Task<Message> SeedMessageAsync(int id, int userId, int? channelId = null, int? chatId = null)
    {
        var message = new Message
        {
            Id = id,
            UserId = userId,
            ChannelId = channelId,
            ChatId = chatId,
            Content = "Hello",
            CreatedAt = DateTime.UtcNow,
        };
        _context.Messages.Add(message);
        await _context.SaveChangesAsync();
        return message;
    }

    [Fact]
    public async Task GetOwnedServersAsync_ReturnsOwnedServers()
    {
        await SeedUserAsync(OwnerId);
        await SeedUserAsync(MemberId);
        await SeedServerAsync(ServerId, OwnerId);
        await SeedServerAsync(OtherServerId, MemberId);

        var result = await _repository.GetOwnedServersAsync(OwnerId);

        Assert.Single(result);
        Assert.Equal(ServerId, result[0].Id);
    }

    [Fact]
    public async Task GetOwnedServersAsync_ReturnsEmpty_WhenNoOwnedServers()
    {
        await SeedUserAsync(OwnerId);
        await SeedServerAsync(ServerId, OwnerId);

        var result = await _repository.GetOwnedServersAsync(MemberId);

        Assert.Empty(result);
    }
    
    [Fact]
    public async Task SearchServersAsync_FindsMatching()
    {
        await SeedUserAsync(OwnerId);
        await SeedServerAsync(ServerId, OwnerId, "Alpha");
        await SeedServerAsync(OtherServerId, OwnerId, "Beta");

        var result = await _repository.SearchServersAsync("Alpha", 10, Ordering.Asc);

        Assert.Single(result);
        Assert.Equal("Alpha", result[0].Name);
    }

    [Fact]
    public async Task SearchServersAsync_ReturnsAll_WhenSearchNameIsNull()
    {
        await SeedUserAsync(OwnerId);
        await SeedServerAsync(ServerId, OwnerId, "Alpha");
        await SeedServerAsync(OtherServerId, OwnerId, "Beta");

        var result = await _repository.SearchServersAsync(null, 10, Ordering.Asc);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task SearchServersAsync_RespectsLimit()
    {
        await SeedUserAsync(OwnerId);
        await SeedServerAsync(ServerId, OwnerId, "Alpha");
        await SeedServerAsync(OtherServerId, OwnerId, "Beta");

        var result = await _repository.SearchServersAsync(null, 1, Ordering.Asc);

        Assert.Single(result);
    }

    [Fact]
    public async Task SearchServersAsync_ReturnsEmpty_WhenNoMatch()
    {
        await SeedUserAsync(OwnerId);
        await SeedServerAsync(ServerId, OwnerId, "Alpha");

        var result = await _repository.SearchServersAsync("xyz", 10, Ordering.Asc);

        Assert.Empty(result);
    }
    
    [Fact]
    public async Task GetServersByUserIdAsync_ReturnsServersForUser()
    {
        await SeedUserAsync(OwnerId);
        await SeedUserAsync(MemberId);
        await SeedServerAsync(ServerId, OwnerId);
        await SeedServerAsync(OtherServerId, OwnerId);
        await SeedUserServerAsync(MemberId, ServerId);

        var result = await _repository.GetServersByUserIdAsync(MemberId);

        Assert.Single(result);
        Assert.Equal(ServerId, result[0].Id);
    }

    [Fact]
    public async Task GetServersByUserIdAsync_ReturnsEmpty_WhenNotMemberOfAny()
    {
        await SeedUserAsync(OwnerId);
        await SeedServerAsync(ServerId, OwnerId);

        var result = await _repository.GetServersByUserIdAsync(MemberId);

        Assert.Empty(result);
    }
    
    [Fact]
    public async Task GetServerByIdAsync_ReturnsServer()
    {
        await SeedUserAsync(OwnerId);
        await SeedServerAsync(ServerId, OwnerId);

        var result = await _repository.GetServerByIdAsync(ServerId);

        Assert.NotNull(result);
        Assert.Equal(ServerId, result.Id);
    }

    [Fact]
    public async Task GetServerByIdAsync_ReturnsNull_WhenNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(async () =>await _repository.GetServerByIdAsync(ServerId));
    }
    
    [Fact]
    public async Task AddServerAsync_AddsServer()
    {
        await SeedUserAsync(OwnerId);

        var server = new Server { Name = "New", Description = "Desc", OwnerId = OwnerId };
        var result = await _repository.AddServerAsync(server);

        Assert.NotNull(result);
        Assert.NotNull(await _context.Servers.FindAsync(result.Id));
    }
    
    [Fact]
    public async Task SaveChangesAsync_PersistsChanges()
    {
        await SeedUserAsync(OwnerId);
        await SeedServerAsync(ServerId, OwnerId, "Original");

        var server = await _context.Servers.FindAsync(ServerId);
        server!.Name = "Updated";
        await _repository.SaveChangesAsync();

        var persisted = await _context.Servers.FindAsync(ServerId);
        Assert.Equal("Updated", persisted!.Name);
    }
    
    [Fact]
    public async Task IsUserMemberAsync_ReturnsTrue_WhenMember()
    {
        await SeedUserAsync(OwnerId);
        await SeedServerAsync(ServerId, OwnerId);
        await SeedUserServerAsync(OwnerId, ServerId);

        var result = await _repository.IsUserMemberAsync(OwnerId, ServerId);

        Assert.True(result);
    }

    [Fact]
    public async Task IsUserMemberAsync_ReturnsFalse_WhenNotMember()
    {
        await SeedUserAsync(OwnerId);
        await SeedServerAsync(ServerId, OwnerId);

        var result = await _repository.IsUserMemberAsync(MemberId, ServerId);

        Assert.False(result);
    }
    
    [Fact]
    public async Task GetUserServerAsync_ReturnsUserServer()
    {
        await SeedUserAsync(OwnerId);
        await SeedServerAsync(ServerId, OwnerId);
        await SeedUserServerAsync(OwnerId, ServerId);

        var result = await _repository.GetUserServerAsync(OwnerId, ServerId);

        Assert.NotNull(result);
        Assert.Equal(OwnerId, result.UserId);
        Assert.Equal(ServerId, result.ServerId);
    }

    [Fact]
    public async Task GetUserServerAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _repository.GetUserServerAsync(OwnerId, ServerId);

        Assert.Null(result);
    }
    
    [Fact]
    public async Task GetServerMemberIdsAsync_ReturnsMemberIds()
    {
        await SeedUserAsync(OwnerId);
        await SeedUserAsync(MemberId);
        await SeedServerAsync(ServerId, OwnerId);
        await SeedUserServerAsync(OwnerId, ServerId);
        await SeedUserServerAsync(MemberId, ServerId);

        var result = await _repository.GetServerMemberIdsAsync(ServerId);

        Assert.Equal(2, result.Count);
        Assert.Contains(OwnerId, result);
        Assert.Contains(MemberId, result);
    }

    [Fact]
    public async Task GetServerMemberIdsAsync_ReturnsEmpty_WhenNoMembers()
    {
        await SeedUserAsync(OwnerId);
        await SeedServerAsync(ServerId, OwnerId);

        var result = await _repository.GetServerMemberIdsAsync(ServerId);

        Assert.Empty(result);
    }
    
    [Fact]
    public async Task GetServerMembersAsync_ReturnsMembers()
    {
        await SeedUserAsync(OwnerId);
        await SeedUserAsync(MemberId);
        await SeedServerAsync(ServerId, OwnerId);
        await SeedUserServerAsync(OwnerId, ServerId);
        await SeedUserServerAsync(MemberId, ServerId);

        var result = await _repository.GetServerMembersAsync(ServerId);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetServerMembersAsync_RespectsLimit()
    {
        await SeedUserAsync(OwnerId);
        await SeedUserAsync(MemberId);
        await SeedServerAsync(ServerId, OwnerId);
        await SeedUserServerAsync(OwnerId, ServerId);
        await SeedUserServerAsync(MemberId, ServerId);

        var result = await _repository.GetServerMembersAsync(ServerId, limit: 1);

        Assert.Single(result);
    }
    
    [Fact]
    public async Task AddUserServerAsync_AddsMembership()
    {
        await SeedUserAsync(OwnerId);
        await SeedUserAsync(MemberId);
        await SeedServerAsync(ServerId, OwnerId);

        await _repository.AddUserServerAsync(new UserServer { UserId = MemberId, ServerId = ServerId });
        
        await Assert.ThrowsAsync<NotFoundException>(async () => await _context.UserServers.FindAsync(MemberId, ServerId));
    }

    [Fact]
    public async Task RemoveUserServerAsync_RemovesMembership()
    {
        await SeedUserAsync(OwnerId);
        await SeedUserAsync(MemberId);
        await SeedServerAsync(ServerId, OwnerId);
        await SeedUserServerAsync(MemberId, ServerId);

        var userServer = await _context.UserServers.FindAsync(MemberId, ServerId);
        await _repository.RemoveUserServerAsync(userServer!);

        var result = await _context.UserServers.FindAsync(MemberId, ServerId);
        Assert.Null(result);
    }
    
    [Fact]
    public async Task GetUserByIdAsync_ReturnsUser()
    {
        await SeedUserAsync(OwnerId);

        var result = await _repository.GetUserByIdAsync(OwnerId);

        Assert.NotNull(result);
        Assert.Equal(OwnerId, result.Id);
    }

    [Fact]
    public async Task GetUserByIdAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _repository.GetUserByIdAsync(OwnerId);

        Assert.Null(result);
    }
    
    [Fact]
    public async Task IsUserBannedAsync_ReturnsTrue_WhenBanned()
    {
        await SeedUserAsync(OwnerId);
        await SeedUserAsync(MemberId);
        await SeedServerAsync(ServerId, OwnerId);
        await SeedBanAsync(MemberId, ServerId);

        var result = await _repository.IsUserBannedAsync(MemberId, ServerId);

        Assert.True(result);
    }

    [Fact]
    public async Task IsUserBannedAsync_ReturnsFalse_WhenNotBanned()
    {
        await SeedUserAsync(OwnerId);
        await SeedServerAsync(ServerId, OwnerId);

        var result = await _repository.IsUserBannedAsync(MemberId, ServerId);

        Assert.False(result);
    }
    
    [Fact]
    public async Task GetBanAsync_ReturnsBan()
    {
        await SeedUserAsync(OwnerId);
        await SeedUserAsync(MemberId);
        await SeedServerAsync(ServerId, OwnerId);
        await SeedBanAsync(MemberId, ServerId);

        var result = await _repository.GetBanAsync(MemberId, ServerId);

        Assert.NotNull(result);
        Assert.Equal(MemberId, result.UserId);
        Assert.Equal(ServerId, result.ServerId);
    }

    [Fact]
    public async Task GetBanAsync_ReturnsNull_WhenNotBanned()
    {
        var result = await _repository.GetBanAsync(MemberId, ServerId);

        Assert.Null(result);
    }
    
    [Fact]
    public async Task GetBannedUsersAsync_ReturnsBannedUsers()
    {
        await SeedUserAsync(OwnerId);
        await SeedUserAsync(MemberId);
        await SeedServerAsync(ServerId, OwnerId);
        await SeedBanAsync(MemberId, ServerId);

        var result = await _repository.GetBannedUsersAsync(ServerId);

        Assert.Single(result);
        Assert.Equal(MemberId, result[0].Id);
    }

    [Fact]
    public async Task GetBannedUsersAsync_ReturnsEmpty_WhenNoBans()
    {
        await SeedUserAsync(OwnerId);
        await SeedServerAsync(ServerId, OwnerId);

        var result = await _repository.GetBannedUsersAsync(ServerId);

        Assert.Empty(result);
    }
    
    [Fact]
    public async Task AddBanAsync_AddsBan()
    {
        await SeedUserAsync(OwnerId);
        await SeedUserAsync(MemberId);
        await SeedServerAsync(ServerId, OwnerId);

        var bus = new BanUserServer { UserId = MemberId, ServerId = ServerId };
        await _repository.AddBanAsync(bus);

        var result = await _context.BanUserServers.FindAsync(bus);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task RemoveBanAsync_RemovesBan()
    {
        await SeedUserAsync(OwnerId);
        await SeedUserAsync(MemberId);
        await SeedServerAsync(ServerId, OwnerId);
        await SeedBanAsync(MemberId, ServerId);

        var ban = await _context.BanUserServers.FindAsync(MemberId, ServerId);
        await _repository.RemoveBanAsync(ban!);

        var result = await _context.BanUserServers.FindAsync(MemberId, ServerId);
        Assert.Null(result);
    }
    

    [Fact]
    public async Task ChannelExistsAsync_ReturnsTrue_WhenExists()
    {
        await SeedUserAsync(OwnerId);
        await SeedServerAsync(ServerId, OwnerId);
        await SeedChannelAsync(ChannelId, ServerId);

        var result = await _repository.ChannelExistsAsync(ChannelId, ServerId);

        Assert.True(result);
    }

    [Fact]
    public async Task ChannelExistsAsync_ReturnsFalse_WhenNotExists()
    {
        var result = await _repository.ChannelExistsAsync(ChannelId, ServerId);

        Assert.False(result);
    }

    [Fact]
    public async Task ChannelExistsAsync_ReturnsFalse_WhenWrongServer()
    {
        await SeedUserAsync(OwnerId);
        await SeedServerAsync(ServerId, OwnerId);
        await SeedServerAsync(OtherServerId, OwnerId);
        await SeedChannelAsync(ChannelId, ServerId);

        var result = await _repository.ChannelExistsAsync(ChannelId, OtherServerId);

        Assert.False(result);
    }
    
    [Fact]
    public async Task GetChannelAsync_ReturnsChannel()
    {
        await SeedUserAsync(OwnerId);
        await SeedServerAsync(ServerId, OwnerId);
        await SeedChannelAsync(ChannelId, ServerId, "Gaming");

        var result = await _repository.GetChannelAsync(ChannelId, ServerId);

        Assert.NotNull(result);
        Assert.Equal(ChannelId, result.Id);
        Assert.Equal("Gaming", result.Name);
    }

    [Fact]
    public async Task GetChannelAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _repository.GetChannelAsync(ChannelId, ServerId);

        Assert.Null(result);
    }
    
    [Fact]
    public async Task GetChannelsByServerIdAsync_ReturnsChannels()
    {
        await SeedUserAsync(OwnerId);
        await SeedServerAsync(ServerId, OwnerId);
        await SeedChannelAsync(ChannelId, ServerId, "General");
        await SeedChannelAsync(2, ServerId, "Gaming");

        var result = await _repository.GetChannelsByServerIdAsync(ServerId);

        Assert.Equal(2, result.Count);
        Assert.All(result, c => Assert.Equal(ServerId, c.ServerId));
    }

    [Fact]
    public async Task GetChannelsByServerIdAsync_ReturnsEmpty_WhenNoChannels()
    {
        await SeedUserAsync(OwnerId);
        await SeedServerAsync(ServerId, OwnerId);

        var result = await _repository.GetChannelsByServerIdAsync(ServerId);

        Assert.Empty(result);
    }
    
    [Fact]
    public async Task GetChannelReturnModelAsync_ReturnsModel()
    {
        await SeedUserAsync(OwnerId);
        await SeedServerAsync(ServerId, OwnerId);
        await SeedChannelAsync(ChannelId, ServerId, "General");

        var result = await _repository.GetChannelReturnModelAsync(ChannelId, ServerId);

        Assert.NotNull(result);
        Assert.Equal(ChannelId, result.Id);
        Assert.Equal(ServerId, result.ServerId);
        Assert.Equal("General", result.Name);
    }

    [Fact]
    public async Task GetChannelReturnModelAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _repository.GetChannelReturnModelAsync(ChannelId, ServerId);

        Assert.Null(result);
    }
    
    [Fact]
    public async Task AddChannelAsync_AddsChannel()
    {
        await SeedUserAsync(OwnerId);
        await SeedServerAsync(ServerId, OwnerId);

        var channel = new Channel { Name = "NewChannel", ServerId = ServerId };
        var result = await _repository.AddChannelAsync(channel);

        Assert.NotNull(result);
        Assert.NotNull(await _context.Channels.FindAsync(result.Id));
    }
    
    [Fact]
    public async Task GetMessageWithRelationsAsync_ReturnsMessage()
    {
        await SeedUserAsync(OwnerId);
        await SeedServerAsync(ServerId, OwnerId);
        await SeedChannelAsync(ChannelId, ServerId);
        await SeedMessageAsync(MessageId, OwnerId, channelId: ChannelId);

        var result = await _repository.GetMessageWithRelationsAsync(MessageId);

        Assert.NotNull(result);
        Assert.Equal(MessageId, result.Id);
        Assert.NotNull(result.User);
        Assert.NotNull(result.Channel);
    }

    [Fact]
    public async Task GetMessageWithRelationsAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _repository.GetMessageWithRelationsAsync(MessageId);

        Assert.Null(result);
    }
    
    [Fact]
    public async Task GetChannelMessagesAsync_ReturnsMessages()
    {
        await SeedUserAsync(OwnerId);
        await SeedServerAsync(ServerId, OwnerId);
        await SeedChannelAsync(ChannelId, ServerId);
        await SeedMessageAsync(MessageId, OwnerId, channelId: ChannelId);
        await SeedMessageAsync(2, OwnerId, channelId: ChannelId);

        var result = await _repository.GetChannelMessagesAsync(ChannelId, new CursorPaginationFilter());

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Data, m => Assert.Equal(ChannelId, m.ChannelId));
    }

    [Fact]
    public async Task GetChannelMessagesAsync_ReturnsEmpty_WhenNoMessages()
    {
        var result = await _repository.GetChannelMessagesAsync(ChannelId, new CursorPaginationFilter());

        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task AddMessageAsync_AddsMessage()
    {
        await SeedUserAsync(OwnerId);
        await SeedServerAsync(ServerId, OwnerId);
        await SeedChannelAsync(ChannelId, ServerId);

        var message = new Message
        {
            UserId = OwnerId,
            ChannelId = ChannelId,
            Content = "Hello",
            CreatedAt = DateTime.UtcNow,
        };

        var result = await _repository.AddMessageAsync(message);

        Assert.NotNull(result);
        Assert.NotNull(await _context.Messages.FindAsync(result.Id));
    }

    [Fact]
    public async Task RemoveMessageAsync_RemovesMessage()
    {
        await SeedUserAsync(OwnerId);
        await SeedServerAsync(ServerId, OwnerId);
        await SeedChannelAsync(ChannelId, ServerId);
        var message = await SeedMessageAsync(MessageId, OwnerId, channelId: ChannelId);

        await _repository.RemoveMessageAsync(message);

        Assert.Null(await _context.Messages.FindAsync(MessageId));
    }
}
