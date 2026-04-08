using System.Security.Cryptography;
using Cyber_Cord.Api.Constants;
using Cyber_Cord.Api.Entities;
using Cyber_Cord.Api.Exceptions;
using Cyber_Cord.Api.Extensions;
using Cyber_Cord.Api.Jobs;
using Cyber_Cord.Api.Models;
using Cyber_Cord.Api.Options;
using Cyber_Cord.Api.Repositories;
using Cyber_Cord.Api.Types.Interfaces;
using Hangfire;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Shared.Models;
using Shared.Types.Interfaces;

namespace Cyber_Cord.Api.Services;

public class UsersService(
    IUsersRepository repository,
    IChatsService chatsService,
    IChatsRepository chatsRepository,
    IServersService serversService,
    IServersRepository serversRepository, 
    IWebSocketHandler wsHandler,
    IMemoryCache memoryCache,
    ICustomEmailSender sender,
    ICustomPasswordHasher hasher,
    IOptions<ActivationCodeOptions> optionsAccessor,
    ICurrentUserContext userContext,
    IUnitOfWork uow,
    IUserJobs userJobs,
    IBackgroundJobClient jobClient
) : IUsersService {
    private const int _randomNumberGeneratorBound = 1_000_000; // 6 places codes
    private const int _accountNotActivatedLifeTimeHours = 24;
    private const int _accountNotActivatedLifeTimeHoursSendEmail = 2;

    private int UserId => userContext.GetId();

    public async Task CheckUserPasswordAsync(int? userId, string? password, bool enforceActivated = true)
    {
        if (userId is null || password is null)
        {
            throw new BadRequestException("Credentials are required");
        }

        var user = await repository.GetUserByIdAsync(userId.Value);

        if (user is null || (enforceActivated && !user.IsActivated))
        {
            throw new NotFoundException($"Could not find an activated user with id {userId}");
        }

        if (!hasher.CheckPassword(password, user.PasswordHash!))
        {
            throw new ForbiddenException("Invalid credentials");
        }
    }

    public async Task EnsureUsersAreFriendsAsync(int userId)
    {
        var result = await repository.UsersAreFriendsAsync(userId, UserId);

        if (!result)
        {
            throw new BadRequestException("Users are not friends");
        }
    }

    public async Task<UserReturnModel> GetUserByIdAsync(int userId)
    {
        var user = await repository.GetUserByIdAsync(userId);

        if (user is null)
        {
            throw new NotFoundException($"Could not find user with id {userId}");
        }

        return user.ToReturnModel();
    }

    public async Task<UserReturnModel> GetCurrentUserAsync()
    {
        var userId = userContext.GetId();

        return await GetUserByIdAsync(userId);
    }

    public async Task<List<UserShortReturnModel>> SearchUsersAsync(UserSearchModel model)
    {
        if (model.SingleResultOnly)
        {
            var user = await repository.SearchSingularAsync(model);

            if (user is null)
            {
                throw new NotFoundException($"Could not find user with those parameters");
            }

            return [user];
        }

        var result = await repository.SearchMultipleAsync(model);

        return result;
    }

    public async Task<UserDetailModel> GetUserDetailAsync(int userId)
    {
        CheckAccessToUser(userId);

        var user = await repository.GetUserDetailAsync(userId);

        if (user is null)
        {
            throw new NotFoundException($"Could not find a user with id {userId}");
        }

        return user;
    }

    public async Task<SettingsReturnModel> GetUsersSettingsAsync(int userId)
    {
        CheckAccessToUser(userId);

        var settings = await repository.GetUsersSettingsAsync(userId);

        if (settings is null)
        {
            throw new NotFoundException($"Could not find settings for a user with id {userId}");
        }

        var returnModel = new SettingsReturnModel
        {
            EnableSounds = settings.EnableSounds
        };

        return returnModel;
    }

    public async Task<List<FriendReturnModel>> SearchFriendsAsync(int userId, FriendSearchModel model)
    {
        CheckAccessToUser(userId);

        if (model.SearchUserId is null)
        {
            var result = await repository.SearchMultipleFriendsAsync(userId, model);

            return result;
        }

        var (requested, received) = await repository.SearchSingularFriendsAsync(userId, model);

        if (requested is not null)
        {
            return [requested];
        }

        if (received is null)
        {
            throw new NotFoundException($"Could not find any friendship with user id {userId}");
        }

        return [received];
    }

    public async Task<List<FriendRequestDetailModel>> GetPendingRequestsAsync(int userId)
    {
        CheckAccessToUser(userId);

        return await repository.GetPendingRequestsAsync(userId);
    }

    public async Task<ChatReturnModel> GetFriendsChatAsync(int userId, int friendshipId)
    {
        CheckAccessToUser(userId);

        if (!await repository.UserHasAccessToFriendshipAsync(userId, friendshipId))
        {
            throw new ForbiddenException($"User does not have access to the friendship with id {friendshipId}");
        }

        var chat = await repository.GetFriendsChatAsync(friendshipId);

        if (chat is null)
        {
            throw new NotFoundException("Could not find a chat friend chat");
        }

        return chat;
    }

    public async Task<UserReturnModel> CreateUserAsync(UserCreateModel model)
    {
        var searchModel = new UserSearchModel
        {
            ActivatedOnly = false,
            SearchName = model.Name
        };

        var user = await uow.ExecuteInTransactionAsync(async Task<User> (transaction) =>
        {
            var existingUser = await repository.SearchSingularAsync(searchModel);

            if (existingUser is not null)
            {
                throw new AlreadyExistsException("User already exists");
            }

            var passwordHash = hasher.CreatePassword(model.Password);

            var user = await repository.CreateUserAsync(model, passwordHash);
            await repository.CreateSettingsForUserAsync(user.Id);

            await repository.AssignRolesToUserAsync(user, RoleNames.User);

            await transaction.CommitAsync();

            return user;
        });

        await SendValidationCodeAsync(user.Id, user.Email!);

        jobClient.Schedule(
            () => userJobs.CheckActivatedUser(user.Id),
            TimeSpan.FromHours(_accountNotActivatedLifeTimeHours)
            );
        
        jobClient.Schedule(
            () => userJobs.CheckActivatedUserSendEmail(user.Id),
            TimeSpan.FromHours(_accountNotActivatedLifeTimeHoursSendEmail)
            );

        var returnModel = user.ToReturnModel();

        return returnModel;
    }

    public async Task ValidateUserAsync(int userId, string validationToken)
    {
        var user = await GetUserOrThrowAsync(userId);

        if (user.IsActivated)
            throw new BadRequestException("User is already activated.");

        if (!memoryCache.TryGetValue(userId, out string? actualToken) || actualToken is null || actualToken != validationToken)
            throw new ForbiddenException("Invalid or expired validation token.");

        memoryCache.Remove(userId);

        await repository.ValidateUserAsync(user);
    }

    public async Task ResendValidationCodeAsync(int userId, string password)
    {
        var user = await GetUserOrThrowAsync(userId);

        await CheckUserPasswordAsync(userId, password, false);

        await SendValidationCodeAsync(user.Id, user.Email!);
    }

    public async Task RequestFriendshipAsync(int userId, FriendRequestCreateModel model)
    {
        CheckAccessToUser(userId);

        if (model.UserId!.Value == userId)
        {
            throw new BadRequestException("You cannot request friendship to yourself");
        }

        if (!await repository.UserExistsAsync(userId))
        {
            throw new NotFoundException($"Could not find user with id {userId}");
        }

        if (await repository.UsersAreFriendsAsync(userId, model.UserId!.Value))
        {
            throw new AlreadyExistsException($"Users already have an unresolved friendrequest or are friends");
        }

        var friendship = await repository.RequestFriendshipAsync(userId, model);

        NotifyUsersAboutFriendRequest(friendship);
    }

    public async Task<FriendRequestDetailModel> AcceptFriendshipAsync(int userId, int friendshipId)
    {
        CheckAccessToUser(userId);

        var returnModel = await uow.ExecuteInTransactionAsync(async (transaction) =>
        {
            var request = await repository.GetPendingRequestAsync(userId, friendshipId);

            if (request is null)
            {
                throw new NotFoundException("No such pending friend request found");
            }

            await repository.AcceptFriendshipAsync(request);

            var returnModel = await AddChatToFriendshipAsync(userId, friendshipId, request, transaction);

            NotifyUsersAboutAcceptedFriendRequest(request);

            return returnModel;
        });

        return returnModel;
    }

    public async Task<UserDetailModel> UpdateUserAsync(int userId, UserUpdateModel model)
    {
        CheckAccessToUser(userId);

        var user = await GetUserOrThrowAsync(userId);

        var returnModel = await repository.UpdateUserAsync(user, model);

        SendGeneralMessageToUser(userId, WebSocketGeneralMessageModel.ScopeType.Settings);

        return returnModel;
    }

    public async Task ChangeUserPasswordAsync(int userId, UserPasswordChangeModel model)
    {
        await CheckUserPasswordAsync(userId, model.OldPassword);

        var user = await GetUserOrThrowAsync(userId);

        string passwordHash = hasher.CreatePassword(model.NewPassword);

        await repository.ChangeUserPasswordAsync(user, passwordHash);
    }

    public async Task<SettingsReturnModel> UpdateUsersSettingsAsync(int userId, JsonPatchDocument<Settings> document)
    {
        CheckAccessToUser(userId);

        var settings = await repository.GetUsersSettingsAsync(userId);

        if (settings is null)
        {
            throw new NotFoundException($"Could not find settings of the user with id {userId}");
        }

        document.ApplyTo(settings);

        if (settings.UserId != userId)
        {
            throw new BadRequestException($"Cannot change user id");
        } 

        await repository.SaveUsersSettingsAsync(settings);

        NotifyUserAboutSettings(userId);

        var returnModel = new SettingsReturnModel
        {
            EnableSounds = settings.EnableSounds
        };

        return returnModel;
    }

    public async Task DeleteUserAsync(int userId, string password)
    {
        await CheckUserPasswordAsync(userId, password);

        var user = await GetUserOrThrowAsync(userId);

        var chats = await  chatsService.SearchChatsAsync(new());
        var friendships = await repository.SearchMultipleFriendsAsync(userId, new());
        var pending = await repository.GetPendingRequestsAsync(userId);
        var servers = await serversRepository.GetServerIdsAsync(userId);

        await uow.ExecuteInTransactionAsync(async Task (transaction) =>
        {
            await repository.RemoveRequestedFriendshipsAsync(userId);
            await repository.RemoveReceivedFriendshipsAsync(userId);

            var serversQuery = await RemoveUsersServersAsync(userId);

            await repository.RemoveUsersUserChatsAsync(userId);
            await repository.RemoveUsersMessagesAsync(userId);
            await chatsRepository.RemoveEmptyChatsAsync();

            await repository.RemoveUsersSettingsAsync(userId);

            await repository.DeleteUserAsync(userId);

            await transaction.CommitAsync();

            SendGeneralMessageToUser(userId, WebSocketConfigurationModel.MessageType.Kill);

            foreach (var action in serversQuery)
            {
                action();
            }
        });

        jobClient.Enqueue(() => userJobs.NotifyAllUsersInUsersChats(chats));
        jobClient.Enqueue(() => userJobs.NotifyUsersFriends(friendships));
        jobClient.Enqueue(() => userJobs.NotifyUsersPendingRequests(userId, pending));
        jobClient.Enqueue(() => userJobs.NotifyUsersServers(servers));

    }

    public async Task RemoveFriendshipAsync(int userId, int friendshipId)
    {
        CheckAccessToUser(userId);

        if (!await repository.UserHasAccessToFriendshipAsync(userId, friendshipId))
        {
            throw new ForbiddenException($"User cannot access the friendship with id {friendshipId}");
        }

        var friendship = await repository.GetFriendshipByIdAsync(friendshipId);

        await repository.RemoveFriendshipAsync(friendshipId);

        NotifyUsersAboutRevokedFriendRequest(friendship);
    }

    public async Task PingAsync(int id)
    {
        await EnsureUsersAreFriendsAsync(id);

        var message = new WebSocketPingModel
        {
            OriginatingUserName = userContext.GetName(),
        };
        
        wsHandler.SendToUser(id, message);
    }

    public async Task AssignRolesToUserAsync(int userId, RolesAssignmentModel model)
    {
        var user = await repository.GetUserByIdAsync(userId);

        if (user is null)
        {
            throw new NotFoundException($"User with id {userId} could not be found");
        }

        await repository.AssignRolesToUserAsync(user, [.. model.Roles]);
    }

    // === End of public API ===

    private void CheckAccessToUser(int userId)
    {
        if (userId != UserId)
        {
            throw new ForbiddenException($"User does not have access to this user's data");
        }
    }
    
    private async Task<User> GetUserOrThrowAsync(int userId)
    {
        var user = await repository.GetUserByIdAsync(userId);

        if (user is null)
        {
            throw new NotFoundException($"Could not find a user with id {userId}");
        }

        return user;
    }

    private async Task<FriendRequestDetailModel> AddChatToFriendshipAsync(int userId, int friendshipId, Friendship friendRequest, IDbContextTransaction transaction)
    {
        var returnModel = new FriendRequestDetailModel
        {
            Id = friendRequest.Id,
            ReceivingUser = friendRequest.ReceivingUser.ToReturnModel(),
            RequestingUser = friendRequest.RequestingUser.ToReturnModel(),
        };

        var foundChat = await repository.GetFriendsChatAsync(friendshipId);

        if (foundChat is not null)
        {
            await transaction.CommitAsync();

            return returnModel;
        }

        var chat = await chatsService.CreateChatAsync(new());

        await chatsService.AddUserToChatAsync(
            chat.Id,
            new() { UserId = friendRequest.RequestingId },
            true
            );

        await transaction.CommitAsync();

        NotifyFriendsAboutChat(friendRequest);

        return returnModel;
    }

    private async Task<List<Action>> RemoveUsersServersAsync(int userId)
    {
        var ownedServers = await serversRepository.GetOwnedServersAsync(userId);

        var tasks = new Task<Action?>[ownedServers.Count];

        foreach (var (index, server) in ownedServers.Index())
        {
            tasks[index] = serversService.DeleteServerCoreAsync(server.Id);
        }

        await Task.WhenAll(tasks);

        await repository.RemoveUsersUserServers(userId);

        return [.. tasks
            .Select(x => x.Result)
            .Where(x => x is not null)
            .Select(x => x!)];
    }

    private async Task SendValidationCodeAsync(int id, string email)
    {
        var tokenValue = RandomNumberGenerator.GetInt32(_randomNumberGeneratorBound);
        var validationToken = tokenValue.ToString("D6");

        memoryCache.Set(id, validationToken, TimeSpan.FromMinutes(optionsAccessor.Value.ActivationCodeLifeTimeInMinutes));

        await sender.SendEmailAsync(email, "Email validation", sender.GetValidationEmailMessage(validationToken, id));
    }

    private void NotifyFriends(Friendship friendship, IWebSocketMessage message)
    {
        wsHandler.SendToUser(friendship.RequestingId, message);
        wsHandler.SendToUser(friendship.ReceivingId, message);
    }

    private void NotifyUsersAboutFriendRequest(Friendship request)
    {
        var message = new WebSocketGeneralMessageModel
        {
            Scope = WebSocketGeneralMessageModel.ScopeType.Request,
        };

        NotifyFriends(request, message);
    }

    private void NotifyFriendsAboutChat(Friendship friendship)
    {
        var message = new WebSocketGeneralMessageModel
        {
            Scope = WebSocketGeneralMessageModel.ScopeType.Chat,
        };

        NotifyFriends(friendship, message);
    }

    private void NotifyUsersAboutAcceptedFriendRequest(Friendship friendship)
    {
        var friendMessage = new WebSocketGeneralMessageModel
        {
            Scope = WebSocketGeneralMessageModel.ScopeType.Friend,
        };
        var requestMessage = new WebSocketGeneralMessageModel
        {
            Scope = WebSocketGeneralMessageModel.ScopeType.Request,
        };

        var message = new WebsocketMessageModel
        {
            Messages = [
                friendMessage,
                requestMessage
                ]
        };

        wsHandler.SendToUser(friendship.RequestingId, message);
        wsHandler.SendToUser(friendship.ReceivingId, message);
    }

    private void NotifyUsersAboutRevokedFriendRequest(Friendship friendship)
    {
        NotifyUsersAboutAcceptedFriendRequest(friendship);
    }

    private void NotifyUserAboutSettings(int userId)
    {
        SendGeneralMessageToUser(userId, WebSocketGeneralMessageModel.ScopeType.Settings);
    }

    private void SendGeneralMessageToUser(int userId, WebSocketGeneralMessageModel.ScopeType scope)
    {
        var message = new WebSocketGeneralMessageModel
        {
            Scope = scope
        };

        wsHandler.SendToUser(userId, message);
    }

    private void SendGeneralMessageToUser(int userId, WebSocketConfigurationModel.MessageType type)
    {
        var message = new WebSocketConfigurationModel
        {
            Type = type
        };

        wsHandler.SendToUser(userId, message);
    }
}
