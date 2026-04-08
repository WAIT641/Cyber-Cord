using Cyber_Cord.Api.Entities;
using Cyber_Cord.Api.Extensions;
using Cyber_Cord.Api.Models;
using Cyber_Cord.Api.Models.Base;
using Cyber_Cord.Api.Repositories;
using Microsoft.EntityFrameworkCore.Storage;
using NSubstitute;
using Shared.Enums;

namespace Cyber_Cord.Api.Tests.Stubs;

public class StubServersRepository : IServersRepository
{
    private readonly StubUserRepository _userRepository;
    
    public readonly Channel Channel1 = new()
    {
        Id = 1,
        Name = "Gaming",
        ServerId = 1
    };

    public readonly Server Server1 = new Server()
    {
        Name = "Server1",
        Description = "Server1 description",
        OwnerId = 1,
        Id = 1,
    };

    public readonly Server Server2 = new()
    {
        Name = "Server2",
        Description = "Server2 description",
        OwnerId = 2,
        Id = 2,
    };

    public readonly UserServer UserServer1 = new()
    {
        ServerId = 1,
        UserId = 1,
    };

    public readonly BanUserServer BanUserServer1 = new()
    {
        UserId = 2,
        ServerId = 1,
    };

    public readonly Message Message1 = new()
    {
        Id = 1,
        ChannelId = 1,
        UserId = 1,
        Content = "Hello, world!",
    };

    public readonly ChannelReturnModel ChannelReturnModel1 = new()
    {
        Id = 1,
        Name = "Gaming",
        ServerId = 1,
    };

    public readonly UserReturnModel UserReturnModel1;

    public StubServersRepository(StubUserRepository userRepository)
    {
        _userRepository = userRepository;
        Server1.Channels.Add(Channel1);
        UserServer1.Server = Server1;
        UserServer1.User = userRepository.User1;
        BanUserServer1.User = userRepository.User2;

        UserReturnModel1 = userRepository.User1.ToReturnModel();
    }

    /// <param name="_">Expecting 1</param>
    public Task<List<Server>> GetOwnedServersAsync(int _) => Task.FromResult<List<Server>>([Server1]);


    /// <param name="searchName">Expecting Server</param>
    /// <param name="limit">Expecting >= 2</param>
    /// <param name="order">Expecting asc</param>
    public Task<List<Server>> SearchServersAsync(string? searchName, int limit, Ordering order) => Task.FromResult<List<Server>>([Server1, Server2]);

    public Task<List<int>> GetServerIdsAsync(int userId) => Task.FromResult<List<int>>([Server1.Id, Server2.Id]);

    /// <param name="userId">Expecting 1</param>
    public Task<List<Server>> GetServersByUserIdAsync(int userId) => Task.FromResult<List<Server>>([Server1, Server2]);

    /// <param name="serverId">Expecting 1 or 2</param>
    public Task<Server> GetServerByIdAsync(int serverId) => serverId == 1 ? Task.FromResult(Server1) : Task.FromResult(Server2);

    public Task<Server> AddServerAsync(Server server) => Task.FromResult(Server1);

    public Task SaveChangesAsync() => Task.CompletedTask;

    public Task DeleteServerAsync(int serverId) => Task.CompletedTask;

    public IDbContextTransaction BeginTransactionAsync() => Substitute.For<IDbContextTransaction>();
    
    /// <param name="userId">Expecting 1</param>
    /// <param name="serverId">Expecting 1</param>
    public Task<bool> IsUserMemberAsync(int userId, int serverId) => Task.FromResult(true);

    public Task<UserServer?> GetUserServerAsync(int userId, int serverId) => Task.FromResult(UserServer1)!;
    
    /// <param name="serverId">Expecting 1</param>
    public Task<List<int>> GetServerMemberIdsAsync(int serverId) => Task.FromResult<List<int>>([1]);
    
    /// <param name="serverId">Expecting 1</param>
    public Task<List<UserReturnModel>> GetServerMembersAsync(int serverId, int limit = Int32.MaxValue) => Task.FromResult<List<UserReturnModel>>([_userRepository.User1.ToReturnModel()]);

    public Task AddUserServerAsync(UserServer userServer) => Task.CompletedTask;

    public Task RemoveUserServerAsync(UserServer userServer) => Task.CompletedTask;

    public Task DeleteUserServersForServerAsync(int serverId) => Task.CompletedTask;

    /// <param name="userId">Expecting 1, returns false; expecting 2, returns true (banned)</param>
    public Task<bool> IsUserBannedAsync(int userId, int serverId) => Task.FromResult(userId == BanUserServer1.UserId);

    /// <param name="userId">Expecting 2</param>
    /// <param name="serverId">Expecting 1</param>
    public Task<BanUserServer?> GetBanAsync(int userId, int serverId) => Task.FromResult(BanUserServer1)!;

    /// <param name="serverId">Expecting 1</param>
    public Task<List<UserReturnModel>> GetBannedUsersAsync(int serverId)
    {
        var banned = _userRepository.User2.ToReturnModel();
        return Task.FromResult<List<UserReturnModel>>([banned]);
    }

    public Task AddBanAsync(BanUserServer ban) => Task.CompletedTask;

    public Task RemoveBanAsync(BanUserServer ban) => Task.CompletedTask;

    public Task DeleteBansForServerAsync(int serverId) => Task.CompletedTask;

    /// <param name="channelId">Expecting 1</param>
    /// <param name="serverId">Expecting 1</param>
    public Task<bool> ChannelExistsAsync(int channelId, int serverId) => Task.FromResult(true);

    /// <param name="channelId">Expecting 1</param>
    /// <param name="serverId">Expecting 1</param>
    public Task<Channel?> GetChannelAsync(int channelId, int serverId) => Task.FromResult(Channel1)!;

    /// <param name="serverId">Expecting 1</param>
    public Task<List<ChannelReturnModel>> GetChannelsByServerIdAsync(int serverId)
        => Task.FromResult<List<ChannelReturnModel>>([ChannelReturnModel1]);

    /// <param name="channelId">Expecting 1</param>
    /// <param name="serverId">Expecting 1</param>
    public Task<ChannelReturnModel?> GetChannelReturnModelAsync(int channelId, int serverId)
        => Task.FromResult(ChannelReturnModel1)!;

    public Task<Channel> AddChannelAsync(Channel channel) => Task.FromResult(Channel1);

    public Task DeleteChannelAsync(int channelId) => Task.CompletedTask;

    public Task DeleteChannelsForServerAsync(int serverId) => Task.CompletedTask;

    /// <param name="messageId">Expecting 1</param>
    public Task<Message?> GetMessageWithRelationsAsync(int messageId) => Task.FromResult(Message1)!;

    /// <param name="channelId">Expecting 1</param>
    public Task<CursorPaginatedResult<MessageReturnModel>> GetChannelMessagesAsync(int channelId, CursorPaginationFilter filter)
    {
        var result = new CursorPaginatedResult<MessageReturnModel>([Message1.ToReturnModel()], 1, 10, DateTime.MaxValue, 1);
        return Task.FromResult(result);
    }

    public Task<Message> AddMessageAsync(Message message) => Task.FromResult(Message1);

    public Task RemoveMessageAsync(Message message) => Task.CompletedTask;

    public Task DeleteMessagesForChannelAsync(int channelId) => Task.CompletedTask;

    public Task DeleteMessagesForServerAsync(int serverId) => Task.CompletedTask;
}