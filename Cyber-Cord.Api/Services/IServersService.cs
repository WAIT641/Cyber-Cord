using Cyber_Cord.Api.Entities;
using Cyber_Cord.Api.Models;
using Cyber_Cord.Api.Models.Base;
using Shared.Enums;

namespace Cyber_Cord.Api.Services;

public interface IServersService
{
    Task EnsureServerAccessAsync(int serverId);
    Task EnsureServerOwnerAsync(int serverId);
    Task<List<Server>> SearchServersAsync(string? searchName, int limit, Ordering order);
    Task<List<ServerReturnModel>> GetAllServersAsync();
    Task<Server> GetServerByIdAsync(int serverId);
    Task<ServerReturnModel> CreateServerAsync(string name, string description);
    Task<ServerReturnModel> UpdateServerAsync(int id, string name, string description);
    Task DeleteServerAsync(int serverId);
    Task<Action?> DeleteServerCoreAsync(int serverId);
    Task<List<UserReturnModel>> GetServerMembersAsync(int serverId, int limit = int.MaxValue);
    Task AddUserToServerAsync(int serverId, int userId);
    Task RemoveUserFromServerAsync(int serverId, int userToRemoveId);
    Task TransferServerOwnershipAsync(int serverId, int newOwnerId);
    Task<List<UserReturnModel>> GetServerBannedUsersAsync(int serverId);
    Task BanUserFromServerAsync(int serverId, int userToBanId);
    Task UnbanUserFromServerAsync(int serverId, int userToBanId);
    Task<List<ChannelReturnModel>> GetServerChannelsAsync(int serverId);
    Task<ChannelReturnModel> GetChannelByIdAsync(int channelId, int serverId);
    Task<ChannelReturnModel> CreateChannelAsync(int serverId, ChannelCreateModel model);
    Task<ChannelReturnModel> UpdateChannelAsync(int channelId, int serverId, ChannelUpdateModel model);
    Task DeleteChannelAsync(int channelId, int serverId);

    Task<CursorPaginatedResult<MessageReturnModel>> GetChannelMessagesAsync(
        int channelId, int serverId, CursorPaginationFilter filter);

    Task<MessageReturnModel> SendMessageAsync(int channelId, int serverId, MessageCreateModel model);

    Task<MessageReturnModel> UpdateChannelMessageAsync(
        int serverId, int channelId, int messageId, MessageUpdateModel model);

    Task DeleteChannelMessageAsync(int serverId, int channelId, int messageId);
}