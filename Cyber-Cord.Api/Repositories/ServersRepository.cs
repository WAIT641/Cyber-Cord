using Cyber_Cord.Api.Data;
using Cyber_Cord.Api.Entities;
using Cyber_Cord.Api.Exceptions;
using Cyber_Cord.Api.Models;
using Cyber_Cord.Api.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Shared.Enums;
using Cyber_Cord.Api.Models.Base;

namespace Cyber_Cord.Api.Repositories;

public class ServersRepository(AppDbContext context) : IServersRepository
{
    public async Task<List<Server>> GetOwnedServersAsync(int userId)
    {
        var servers = await context.Servers
            .Where(x => x.OwnerId == userId)
            .ToListAsync();

        return servers;
    }

    public async Task<List<Server>> SearchServersAsync(string? searchName, int limit, Ordering order) =>
        await context.Servers
            .AsNoTracking()
            .Include(x => x.UserServers)
            .Where(s => s.Name.Contains(searchName ?? string.Empty))
            .OrderBy(order, x => x.Name)
            .Take(limit)
            .ToListAsync();

    public async Task<List<int>> GetServerIdsAsync(int userId) =>
        await context.UserServers
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.ServerId)
            .ToListAsync();

    public async Task<List<Server>> GetServersByUserIdAsync(int userId) =>
        await context.UserServers
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Include(x => x.Server)
            .Select(x => x.Server)
            .ToListAsync();

    public async Task<Server> GetServerByIdAsync(int serverId) =>
        await context.Servers.FirstOrDefaultAsync(s => s.Id == serverId) ?? throw new NotFoundException($"Could not find server with id {serverId}");

    public async Task<Server> AddServerAsync(Server server)
    {
        context.Servers.Add(server);
        await context.SaveChangesAsync();
        return server;
    }

    public async Task SaveChangesAsync() =>
        await context.SaveChangesAsync();

    public async Task DeleteServerAsync(int serverId) =>
        await context.Servers
            .Where(x => x.Id == serverId)
            .ExecuteDeleteAsync();

    public IDbContextTransaction BeginTransactionAsync() =>
        context.Database.BeginTransaction();
    
    public async Task<bool> IsUserMemberAsync(int userId, int serverId) =>
        await context.UserServers
            .AnyAsync(x => x.UserId == userId && x.ServerId == serverId);

    public async Task<UserServer?> GetUserServerAsync(int userId, int serverId) =>
        await context.UserServers
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ServerId == serverId);

    public async Task<List<int>> GetServerMemberIdsAsync(int serverId) =>
        await context.UserServers
            .AsNoTracking()
            .Where(us => us.ServerId == serverId)
            .Select(us => us.UserId)
            .ToListAsync();

    public async Task<List<UserReturnModel>> GetServerMembersAsync(int serverId, int limit = int.MaxValue) =>
        await context.UserServers
            .AsNoTracking()
            .Where(x => x.ServerId == serverId)
            .Include(x => x.User)
            .Select(x => x.User)
            .Take(limit)
            .Select(x => x.ToReturnModel())
            .ToListAsync();

    public async Task AddUserServerAsync(UserServer userServer)
    {
        context.UserServers.Add(userServer);
        await context.SaveChangesAsync();
    }

    public async Task RemoveUserServerAsync(UserServer userServer)
    {
        context.UserServers.Remove(userServer);
        await context.SaveChangesAsync();
    }

    public async Task DeleteUserServersForServerAsync(int serverId) =>
        await context.UserServers
            .Where(x => x.ServerId == serverId)
            .ExecuteDeleteAsync();
    
    public async Task<User?> GetUserByIdAsync(int userId) =>
        await context.Users.FirstOrDefaultAsync(x => x.Id == userId);

    public async Task<bool> IsUserBannedAsync(int userId, int serverId) =>
        await context.BanUserServers
            .AnyAsync(x => x.UserId == userId && x.ServerId == serverId);

    public async Task<BanUserServer?> GetBanAsync(int userId, int serverId) =>
        await context.BanUserServers
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ServerId == serverId);

    public async Task<List<UserReturnModel>> GetBannedUsersAsync(int serverId) =>
        await context.BanUserServers
            .Where(x => x.ServerId == serverId)
            .Include(x => x.User)
            .Select(x => x.User.ToReturnModel())
            .ToListAsync();

    public async Task AddBanAsync(BanUserServer ban)
    {
        context.BanUserServers.Add(ban);
        await context.SaveChangesAsync();
    }

    public async Task RemoveBanAsync(BanUserServer ban)
    {
        context.BanUserServers.Remove(ban);
        await context.SaveChangesAsync();
    }

    public async Task DeleteBansForServerAsync(int serverId) =>
        await context.BanUserServers
            .Where(x => x.ServerId == serverId)
            .ExecuteDeleteAsync();
    
    public async Task<bool> ChannelExistsAsync(int channelId, int serverId) =>
        await context.Channels
            .AnyAsync(c => c.Id == channelId && c.ServerId == serverId);

    public async Task<Channel?> GetChannelAsync(int channelId, int serverId) =>
        await context.Channels
            .FirstOrDefaultAsync(c => c.Id == channelId && c.ServerId == serverId);

    public async Task<List<ChannelReturnModel>> GetChannelsByServerIdAsync(int serverId) =>
        await context.Channels
            .Where(c => c.ServerId == serverId)
            .OrderBy(c => c.Id)
            .Select(c => new ChannelReturnModel
            {
                Id = c.Id,
                ServerId = c.ServerId,
                Name = c.Name,
                Description = c.Description
            })
            .ToListAsync();

    public async Task<ChannelReturnModel?> GetChannelReturnModelAsync(int channelId, int serverId) =>
        await context.Channels
            .Where(c => c.Id == channelId && c.ServerId == serverId)
            .Select(c => new ChannelReturnModel
            {
                Id = c.Id,
                ServerId = c.ServerId,
                Name = c.Name,
                Description = c.Description
            })
            .FirstOrDefaultAsync();

    public async Task<Channel> AddChannelAsync(Channel channel)
    {
        context.Channels.Add(channel);
        await context.SaveChangesAsync();
        return channel;
    }

    public async Task DeleteChannelAsync(int channelId) =>
        await context.Channels
            .Where(x => x.Id == channelId)
            .ExecuteDeleteAsync();

    public async Task DeleteChannelsForServerAsync(int serverId) =>
        await context.Channels
            .Where(x => x.ServerId == serverId)
            .ExecuteDeleteAsync();
    
    public async Task<Message?> GetMessageWithRelationsAsync(int messageId) =>
        await context.Messages
            .Include(m => m.User)
            .Include(m => m.Channel)
            .Include(m => m.Chat)
            .ThenInclude(c => c!.UserChats)
            .FirstOrDefaultAsync(m => m.Id == messageId);

    public async Task<CursorPaginatedResult<MessageReturnModel>> GetChannelMessagesAsync(
        int channelId, CursorPaginationFilter filter) =>
        await context.Messages
            .Where(m => m.ChannelId == channelId)
            .Select(m => new MessageReturnModel
            {
                Id = m.Id,
                ChannelId = m.ChannelId,
                ChatId = m.ChatId,
                UserId = m.UserId,
                Content = m.Content,
                CreatedAt = m.CreatedAt,
            })
            .ToCursorPaginatedAsync(filter);

    public async Task<Message> AddMessageAsync(Message message)
    {
        context.Messages.Add(message);
        await context.SaveChangesAsync();
        return message;
    }

    public async Task RemoveMessageAsync(Message message)
    {
        context.Messages.Remove(message);
        await context.SaveChangesAsync();
    }

    public async Task DeleteMessagesForChannelAsync(int channelId) =>
        await context.Messages
            .Where(x => x.ChannelId == channelId)
            .ExecuteDeleteAsync();

    public async Task DeleteMessagesForServerAsync(int serverId) =>
        await context.Messages
            .Where(x => x.ChannelId != null)
            .Include(x => x.Channel)
            .Where(x => x.Channel!.ServerId == serverId)
            .ExecuteDeleteAsync();
}