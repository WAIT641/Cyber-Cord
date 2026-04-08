using Cyber_Cord.Api.Entities;
using Cyber_Cord.Api.Exceptions;
using Cyber_Cord.Api.Models;
using Cyber_Cord.Api.Models.Base;
using Cyber_Cord.Api.Repositories;
using Shared.Enums;
using Shared.Models;
using Shared.Types.Interfaces;

namespace Cyber_Cord.Api.Services;

public class ServersService(IServersRepository repository, IWebSocketHandler handler, ICurrentUserContext userContext, IUsersRepository usersRepository) : IServersService
{
    // ── Access guards ─────────────────────────────────────────────────────────

    public async Task EnsureServerAccessAsync(int serverId)
    {
        var userId = userContext.GetId();
        var server = await repository.GetServerByIdAsync(serverId)
                     ?? throw new NotFoundException($"Could not find server with id {serverId}");

        if (server.OwnerId == userId)
            return;

        var isMember = await repository.IsUserMemberAsync(userId, serverId);
        if (!isMember)
            throw new ForbiddenException("User does not have access to this server");
    }

    public async Task EnsureServerOwnerAsync(int serverId)
    {
        var userId = userContext.GetId();
        var server = await repository.GetServerByIdAsync(serverId)
                     ?? throw new NotFoundException($"Could not find server with id {serverId}");

        if (server.OwnerId != userId)
            throw new ForbiddenException("Only the server owner can perform this action");
    }

    // ── Servers ───────────────────────────────────────────────────────────────

    public async Task<List<Server>> SearchServersAsync(string? searchName, int limit, Ordering order) =>
        await repository.SearchServersAsync(searchName, limit, order);

    public async Task<List<ServerReturnModel>> GetAllServersAsync()
    {
        var userId = userContext.GetId();
        var servers = await repository.GetServersByUserIdAsync(userId);

        return servers.Select(x => new ServerReturnModel
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            OwnerId = x.OwnerId
        }).ToList();
    }

    public async Task<Server> GetServerByIdAsync(int serverId)
    {
        await EnsureServerAccessAsync(serverId);

        return await repository.GetServerByIdAsync(serverId)
               ?? throw new NotFoundException($"Could not find server with id {serverId}");
    }

    public async Task<ServerReturnModel> CreateServerAsync(string name, string description)
    {
        var ownerId = userContext.GetId();

        _ = await usersRepository.GetUserByIdAsync(ownerId)
            ?? throw new NotFoundException($"User {ownerId} not found.");

        await using var transaction = repository.BeginTransactionAsync();
        
        var server = await repository.AddServerAsync(new Server
        {
            Name = name,
            Description = description,
            OwnerId = ownerId
        });

        await repository.AddUserServerAsync(new UserServer
        {
            ServerId = server.Id,
            UserId = ownerId
        });

        await transaction.CommitAsync();

        NotifyUserAboutServerPresence(ownerId);

        return new ServerReturnModel
        {
            Id = server.Id,
            Name = server.Name,
            Description = server.Description,
            OwnerId = server.OwnerId
        };
    }

    public async Task<ServerReturnModel> UpdateServerAsync(int id, string name, string description)
    {
        await EnsureServerAccessAsync(id);

        var server = await repository.GetServerByIdAsync(id)
                     ?? throw new NotFoundException($"Could not find server with id {id}");

        server.Name = name;
        server.Description = description;

        await repository.SaveChangesAsync();
        await NotifyUsersAboutServerUpdateAsync(id);

        return new ServerReturnModel
        {
            Id = server.Id,
            Name = server.Name,
            Description = server.Description,
            OwnerId = server.OwnerId
        };
    }

    public async Task DeleteServerAsync(int serverId)
    {
        await EnsureServerAccessAsync(serverId);
        await EnsureServerOwnerAsync(serverId);

        await using var transaction = repository.BeginTransactionAsync();
        
        var action = await DeleteServerCoreAsync(serverId);
        await transaction.CommitAsync();
        action?.Invoke();
    }

    // Unwrapped version for use inside an existing transaction (e.g. cascade deletes).
    public async Task<Action?> DeleteServerCoreAsync(int serverId)
    {
        _ = await repository.GetServerByIdAsync(serverId);

        var action = await QueryUsersAboutRemovalAsync(serverId);

        await repository.DeleteMessagesForServerAsync(serverId);
        await repository.DeleteChannelsForServerAsync(serverId);
        await repository.DeleteBansForServerAsync(serverId);
        await repository.DeleteUserServersForServerAsync(serverId);
        await repository.DeleteServerAsync(serverId);

        return action;
    }

    // ── Members ───────────────────────────────────────────────────────────────

    public async Task<List<UserReturnModel>> GetServerMembersAsync(int serverId, int limit = int.MaxValue) =>
        await repository.GetServerMembersAsync(serverId, limit);

    public async Task AddUserToServerAsync(int serverId, int userId)
    {
        await EnsureServerAccessAsync(serverId);

        _ = await repository.GetServerByIdAsync(serverId)
            ?? throw new NotFoundException($"Could not find server with id {serverId}");

        _ = await usersRepository.GetUserByIdAsync(userId)
            ?? throw new NotFoundException($"Could not find user with id {userId}");

        if (await repository.IsUserBannedAsync(userId, serverId))
            throw new ForbiddenException($"User {userId} is banned from server {serverId}");

        if (await repository.IsUserMemberAsync(userId, serverId))
            throw new AlreadyExistsException($"User {userId} is already a member of server {serverId}");

        var action = await QueryUsersAboutChangedUserPresenceAsync(serverId);

        await repository.AddUserServerAsync(new UserServer { UserId = userId, ServerId = serverId });

        NotifyUserAboutServerPresence(userId);
        action();
    }

    public async Task RemoveUserFromServerAsync(int serverId, int userToRemoveId)
    {
        await EnsureServerAccessAsync(serverId);
        await EnsureServerOwnerAsync(serverId);

        var userId = userContext.GetId();

        if (userId == userToRemoveId)
            throw new BadRequestException("You cannot remove yourself from the server.");

        var userServer = await repository.GetUserServerAsync(userToRemoveId, serverId)
                         ?? throw new NotFoundException($"User {userToRemoveId} is not a member of server {serverId}.");

        await repository.RemoveUserServerAsync(userServer);

        NotifyUserAboutServerPresence(userToRemoveId);
        await NotifyUsersAboutChangedUserPresenceAsync(serverId);
    }

    public async Task TransferServerOwnershipAsync(int serverId, int newOwnerId)
    {
        await EnsureServerOwnerAsync(serverId);

        var currentUserId = userContext.GetId();

        if (currentUserId == newOwnerId)
            throw new BadRequestException("You are already the owner of this server.");

        var server = await repository.GetServerByIdAsync(serverId)
                     ?? throw new NotFoundException($"Could not find server with id {serverId}");

        if (!await repository.IsUserMemberAsync(newOwnerId, serverId))
            throw new BadRequestException($"User {newOwnerId} is not a member of this server.");

        server.OwnerId = newOwnerId;
        await repository.SaveChangesAsync();

        await NotifyUsersAboutServerUpdateAsync(serverId);
    }
    
    // ── Bans ──────────────────────────────────────────────────────────────────

    public async Task<List<UserReturnModel>> GetServerBannedUsersAsync(int serverId)
    {
        await EnsureServerAccessAsync(serverId);
        return await repository.GetBannedUsersAsync(serverId);
    }

    public async Task BanUserFromServerAsync(int serverId, int userToBanId)
    {
        await EnsureServerAccessAsync(serverId);
        await EnsureServerOwnerAsync(serverId);

        var userId = userContext.GetId();

        if (userId == userToBanId)
            throw new BadRequestException("You cannot ban yourself");

        _ = await usersRepository.GetUserByIdAsync(userToBanId)
            ?? throw new NotFoundException($"Could not find user with id {userToBanId}");

        if (await repository.IsUserBannedAsync(userToBanId, serverId))
            throw new BadRequestException($"User {userToBanId} is already banned from server {serverId}.");

        var userServer = await repository.GetUserServerAsync(userToBanId, serverId);
        if (userServer is not null)
            await repository.RemoveUserServerAsync(userServer);

        await repository.AddBanAsync(new BanUserServer { UserId = userToBanId, ServerId = serverId });

        NotifyUserAboutServerPresence(userToBanId);
        await NotifyUsersAboutChangedUserPresenceAsync(serverId);
    }

    public async Task UnbanUserFromServerAsync(int serverId, int userToBanId)
    {
        await EnsureServerAccessAsync(serverId);
        await EnsureServerOwnerAsync(serverId);

        var ban = await repository.GetBanAsync(userToBanId, serverId)
                  ?? throw new NotFoundException($"User {userToBanId} is not banned from server {serverId}.");

        await repository.RemoveBanAsync(ban);
        await NotifyUsersAboutChangedUserPresenceAsync(serverId);
    }

    // ── Channels ──────────────────────────────────────────────────────────────

    public async Task<List<ChannelReturnModel>> GetServerChannelsAsync(int serverId)
    {
        await EnsureServerAccessAsync(serverId);
        return await repository.GetChannelsByServerIdAsync(serverId);
    }

    public async Task<ChannelReturnModel> GetChannelByIdAsync(int channelId, int serverId)
    {
        await EnsureServerAccessAsync(serverId);

        return await repository.GetChannelReturnModelAsync(channelId, serverId)
               ?? throw new NotFoundException($"Could not find channel with id {channelId}");
    }

    public async Task<ChannelReturnModel> CreateChannelAsync(int serverId, ChannelCreateModel model)
    {
        await EnsureServerOwnerAsync(serverId);

        var channel = await repository.AddChannelAsync(new Channel
        {
            ServerId = serverId,
            Name = model.Name,
            Description = model.Description ?? string.Empty
        });

        await NotifyUsersAboutChannelAsync(serverId);

        return new ChannelReturnModel
        {
            Id = channel.Id,
            ServerId = channel.ServerId,
            Name = channel.Name,
            Description = channel.Description
        };
    }

    public async Task<ChannelReturnModel> UpdateChannelAsync(int channelId, int serverId, ChannelUpdateModel model)
    {
        await EnsureServerOwnerAsync(serverId);

        var channel = await repository.GetChannelAsync(channelId, serverId)
                      ?? throw new NotFoundException($"Could not find channel with id {channelId}");

        channel.Name = model.Name;
        channel.Description = model.Description ?? channel.Description;

        await repository.SaveChangesAsync();
        await NotifyUsersAboutChannelAsync(serverId);

        return new ChannelReturnModel
        {
            Id = channel.Id,
            ServerId = channel.ServerId,
            Name = channel.Name,
            Description = channel.Description
        };
    }

    public async Task DeleteChannelAsync(int channelId, int serverId)
    {
        await using var transaction = repository.BeginTransactionAsync();
        
        await EnsureServerOwnerAsync(serverId);

        var channel = await repository.GetChannelAsync(channelId, serverId);
        if (channel is null)
            return;

        await repository.DeleteMessagesForChannelAsync(channelId);
        await repository.DeleteChannelAsync(channelId);

        await transaction.CommitAsync();
        await NotifyUsersAboutChannelAsync(serverId);
    }

    // ── Messages ──────────────────────────────────────────────────────────────

    public async Task<CursorPaginatedResult<MessageReturnModel>> GetChannelMessagesAsync(
        int channelId, int serverId, CursorPaginationFilter filter)
    {
        await EnsureServerAccessAsync(serverId);
        return await repository.GetChannelMessagesAsync(channelId, filter);
    }

    public async Task<MessageReturnModel> SendMessageAsync(int channelId, int serverId, MessageCreateModel model)
    {
        var userId = userContext.GetId();

        await EnsureServerAccessAsync(serverId);

        if (!await repository.ChannelExistsAsync(channelId, serverId))
            throw new NotFoundException($"Channel {channelId} not found in server {serverId}.");

        var message = await repository.AddMessageAsync(new Message
        {
            ChannelId = channelId,
            UserId = userId,
            Content = model.Content,
            CreatedAt = DateTime.UtcNow
        });

        await NotifyServerMembersAboutMessageAsync(serverId, channelId, message.Id, WebSocketMessageActionModel.ActionType.Received);

        return new MessageReturnModel
        {
            Id = message.Id,
            ChannelId = message.ChannelId,
            ChatId = message.ChatId,
            UserId = message.UserId,
            Content = message.Content,
            CreatedAt = message.CreatedAt,
        };
    }

    public async Task<MessageReturnModel> UpdateChannelMessageAsync(
        int serverId, int channelId, int messageId, MessageUpdateModel model)
    {
        var userId = userContext.GetId();

        await EnsureServerAccessAsync(serverId);
        await GetChannelByIdAsync(channelId, serverId); // throws NotFoundException if missing

        var message = await repository.GetMessageWithRelationsAsync(messageId)
                      ?? throw new NotFoundException($"Could not find message with id {messageId}");

        if (message.UserId != userId)
            throw new ForbiddenException("You can only edit your own messages.");

        message.Content = model.Content;
        await repository.SaveChangesAsync();

        await NotifyServerMembersAboutMessageAsync(serverId, channelId, message.Id, WebSocketMessageActionModel.ActionType.Altered);

        return new MessageReturnModel
        {
            Id = message.Id,
            ChannelId = message.ChannelId,
            ChatId = message.ChatId,
            UserId = message.UserId,
            Content = message.Content,
            CreatedAt = message.CreatedAt
        };
    }

    public async Task DeleteChannelMessageAsync(int serverId, int channelId, int messageId)
    {
        var userId = userContext.GetId();

        var server = await GetServerByIdAsync(serverId);
        await EnsureServerAccessAsync(serverId);

        var channelExists = await repository.ChannelExistsAsync(channelId, serverId);
        if (!channelExists)
            return;

        var message = await repository.GetMessageWithRelationsAsync(messageId);
        if (message is null)
            return;

        var canDelete = message.UserId == userId || server.OwnerId == userId;
        if (!canDelete)
            throw new ForbiddenException("You do not have permission to delete this message.");

        await repository.RemoveMessageAsync(message);

        await NotifyServerMembersAboutMessageAsync(serverId, channelId, message.Id, WebSocketMessageActionModel.ActionType.Removed);
    }

    private void NotifyUserAboutServerPresence(int userId)
    {
        var message = new WebSocketGeneralMessageModel
        {
            Scope = WebSocketGeneralMessageModel.ScopeType.Server
        };

        handler.SendToUser(userId, message);
    }

    private async Task NotifyUsersAboutChangeAsync(int serverId, IWebSocketMessage message)
    {
        var action = await QueryUsersToNotifyAboutChangeAsync(serverId, message);

        action();
    }

    private async Task NotifyServerMembersAboutMessageAsync(
        int serverId,
        int channelId,
        int messageId,
        WebSocketMessageActionModel.ActionType actionType
    ) {
        var message = new WebSocketMessageActionModel
        {
            ChannelId = channelId,
            Action = actionType,
            MessageId = messageId
        };

        var members = await repository.GetServerMembersAsync(serverId);

        foreach (var member in members)
        {
            handler.SendToUser(member.Id, message);
        }
    }

    private async Task<Action> QueryUsersToNotifyAboutChangeAsync(int serverId, IWebSocketMessage message)
    {
        var users = await GetServerMembersAsync(serverId);

        return () =>
        {
            foreach (var user in users)
            {
                handler.SendToUser(user.Id, message);
            }
        };
    }

    private async Task<Action> QueryUsersAboutRemovalAsync(int serverId)
    {
        var message = new WebSocketGeneralMessageModel
        {
            Scope = WebSocketGeneralMessageModel.ScopeType.Server
        };

        return await QueryUsersToNotifyAboutChangeAsync(serverId, message);
    }

    private async Task<Action> QueryUsersAboutChangedUserPresenceAsync(int serverId)
    {
        var message = new WebSocketServerModel
        {
            ServerId = serverId,
            Scope = WebSocketServerModel.ScopeType.User
        };

        return await QueryUsersToNotifyAboutChangeAsync(serverId, message);
    }

    private async Task NotifyUsersAboutChangedUserPresenceAsync(int serverId)
    {
        var action = await QueryUsersAboutChangedUserPresenceAsync(serverId);
        action();
    }

    private async Task NotifyUsersAboutServerUpdateAsync(int serverId)
    {
        var message = new WebSocketServerModel
        {
            ServerId = serverId,
            Scope = WebSocketServerModel.ScopeType.User
        };

        await NotifyUsersAboutChangeAsync(serverId, message);
    }

    private async Task NotifyUsersAboutChannelAsync(int serverId)
    {
        var message = new WebSocketServerModel
        {
            ServerId = serverId,
            Scope = WebSocketServerModel.ScopeType.Channel
        };

        await NotifyUsersAboutChangeAsync(serverId, message);
    }
}
