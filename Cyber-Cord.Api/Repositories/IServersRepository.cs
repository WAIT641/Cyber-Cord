using Cyber_Cord.Api.Entities;
using Cyber_Cord.Api.Models;
using Cyber_Cord.Api.Models.Base;
using Microsoft.EntityFrameworkCore.Storage;
using Shared.Enums;

namespace Cyber_Cord.Api.Repositories;

public interface IServersRepository
{
    Task<List<Server>> GetOwnedServersAsync(int userId);
    Task<List<Server>> SearchServersAsync(string? searchName, int limit, Ordering order);
    Task<List<Server>> GetServersByUserIdAsync(int userId);
    Task<Server> GetServerByIdAsync(int serverId);
    Task<List<int>> GetServerIdsAsync(int userId);
    Task<Server> AddServerAsync(Server server);
    Task SaveChangesAsync();
    Task DeleteServerAsync(int serverId);
    IDbContextTransaction BeginTransactionAsync();
    Task<bool> IsUserMemberAsync(int userId, int serverId);
    Task<UserServer?> GetUserServerAsync(int userId, int serverId);
    Task<List<int>> GetServerMemberIdsAsync(int serverId);
    Task<List<UserReturnModel>> GetServerMembersAsync(int serverId, int limit = int.MaxValue);
    Task AddUserServerAsync(UserServer userServer);
    Task RemoveUserServerAsync(UserServer userServer);
    Task DeleteUserServersForServerAsync(int serverId);
    Task<bool> IsUserBannedAsync(int userId, int serverId);
    Task<BanUserServer?> GetBanAsync(int userId, int serverId);
    Task<List<UserReturnModel>> GetBannedUsersAsync(int serverId);
    Task AddBanAsync(BanUserServer ban);
    Task RemoveBanAsync(BanUserServer ban);
    Task DeleteBansForServerAsync(int serverId);
    Task<bool> ChannelExistsAsync(int channelId, int serverId);
    Task<Channel?> GetChannelAsync(int channelId, int serverId);
    Task<List<ChannelReturnModel>> GetChannelsByServerIdAsync(int serverId);
    Task<ChannelReturnModel?> GetChannelReturnModelAsync(int channelId, int serverId);
    Task<Channel> AddChannelAsync(Channel channel);
    Task DeleteChannelAsync(int channelId);
    Task DeleteChannelsForServerAsync(int serverId);
    Task<Message?> GetMessageWithRelationsAsync(int messageId);

    Task<CursorPaginatedResult<MessageReturnModel>> GetChannelMessagesAsync(
        int channelId, CursorPaginationFilter filter);

    Task<Message> AddMessageAsync(Message message);
    Task RemoveMessageAsync(Message message);
    Task DeleteMessagesForChannelAsync(int channelId);
    Task DeleteMessagesForServerAsync(int serverId);
}