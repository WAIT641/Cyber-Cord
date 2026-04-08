using System.Drawing;
using Cyber_Cord.Api.Data;
using Cyber_Cord.Api.Entities;
using Cyber_Cord.Api.Extensions;
using Cyber_Cord.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace Cyber_Cord.Api.Repositories;

public class UsersRepository(AppDbContext context, UserManager<User> manager) : IUsersRepository
{     
    public async Task<User?> GetUserByIdAsync(int id)
    {
        var user = await manager.FindByIdAsync(id.ToString());

        return user;
    }

    public async Task<Friendship> GetFriendshipByIdAsync(int id)
    {
        return await context.Friendships.FirstAsync(f => f.Id == id);
    }

    public async Task<bool> UserHasAccessToFriendshipAsync(int userId, int friendshipId)
    {
        return await context.Friendships
            .AsNoTracking()
            .AnyAsync(f => f.Id == friendshipId && (f.ReceivingId == userId || f.RequestingId == userId));
    }

    public async Task<bool> UserExistsAsync(int userId)
    {
        return await context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == userId);
    }

    public async Task<bool> UsersAreFriendsAsync(int firstUser, int secondUser)
    {
        var friendship = await context.Friendships
            .AsNoTracking()
            .AnyAsync(f
            => f.ReceivingId == firstUser && f.RequestingId == secondUser
            || f.RequestingId == firstUser && f.ReceivingId == secondUser
            );

        return friendship;
    }

    public async Task<UserDetailModel?> GetUserDetailAsync(int userId)
    {
        var result = await context.Users
            .AsNoTracking()
            .Select(x => new UserDetailModel
            {
                Id = x.Id,
                Name = x.UserName!,
                DisplayName = x.DisplayName,
                CreatedAt = x.CreatedAt,
                Description = x.Description,
                BannerColor = new ColorReturnModel
                {
                    Red = x.BannerColor.R,
                    Green = x.BannerColor.G,
                    Blue = x.BannerColor.B
                },
                Email = x.Email!,
                IsActivated = x.IsActivated
            })
            .FirstOrDefaultAsync(x => x.Id == userId);
        
        return result;
    }

    public async Task<Settings?> GetUsersSettingsAsync(int userId)
    {
        var result = await context.Settings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId);

        return result;
    }

    public async Task<List<FriendRequestDetailModel>> GetPendingRequestsAsync(int userId)
    {
        var result = await context.Friendships
            .AsNoTracking()
            .Include(f => f.RequestingUser)
            .Include(f => f.ReceivingUser)
            .Where(f => f.ReceivingId == userId || f.RequestingId == userId)
            .Where(f => f.Pending == true)
            .Select(f => new FriendRequestDetailModel
            {
                Id = f.Id,
                ReceivingUser = f.ReceivingUser.ToReturnModel(),
                RequestingUser = f.RequestingUser.ToReturnModel()
            })
            .ToListAsync();

        return result;
    }

    public async Task<ChatReturnModel?> GetFriendsChatAsync(int friendshipId)
    {
        var friendship = await context.Friendships
            .AsNoTracking()
            .FirstAsync(x => x.Id == friendshipId);

        var chat = await context.Chats
            .AsNoTracking()
            .Include(x => x.UserChats)
            .Where(x
                => x.UserChats.Any(uc => uc.UserId == friendship.ReceivingId)
                && x.UserChats.Any(uc => uc.UserId == friendship.RequestingId)
                )
            .Select(x => new ChatReturnModel
            {
                Id = x.Id,
                Name = x.Name,
            })
            .FirstOrDefaultAsync(x => x.Name == null);

        return chat;
    }

    public async Task<Friendship?> GetPendingRequestAsync(int userId, int friendshipId)
    {
        var friendRequest = await context.Friendships
            .Include(x => x.RequestingUser)
            .Include(x => x.ReceivingUser)
            .FirstOrDefaultAsync(x => x.Id == friendshipId && x.ReceivingId == userId && x.Pending == true);

        return friendRequest;
    }

    public async Task<List<UserShortReturnModel>> SearchMultipleAsync(UserSearchModel model)
    {
        var result = await context.Users
            .AsNoTracking()
            .Where(x => x.UserName!.Contains(model.SearchName ?? string.Empty))
            .Where(x => !model.ActivatedOnly || x.IsActivated)
            .OrderBy(model.Order!.Value, x => x.UserName!)
            .Take(model.Limit)
            .Select(x => new UserShortReturnModel
            {
                Id = x.Id,
                Name = x.UserName!,
                DisplayName = x.DisplayName
            })
            .ToListAsync();

        return result;
    }

    public async Task<UserShortReturnModel?> SearchSingularAsync(UserSearchModel model)
    {
        var result = await context.Users
            .AsNoTracking()
            .Where(x => !model.ActivatedOnly || x.IsActivated)
            .Select(x => new UserShortReturnModel
            {
                Id = x.Id,
                Name = x.UserName!,
                DisplayName = x.DisplayName,
            })
            .FirstOrDefaultAsync(x => x.Name == model.SearchName);

        return result;
    }

    public async Task<List<FriendReturnModel>> SearchMultipleFriendsAsync(int userId, FriendSearchModel model)
    {
        var requests = await context.Friendships
            .AsNoTracking()
            .Include(f => f.ReceivingUser)
            .Where(f => f.RequestingId == userId)
            .Where(f => f.Pending == false)
            .Where(f => f.ReceivingUser.UserName!.Contains(model.SearchName ?? string.Empty))
            .Take(model.Limit)
            .Select(f => new FriendReturnModel
            {
                Id = f.Id,
                OtherUser = f.ReceivingUser.ToReturnModel()
            })
            .ToListAsync();

        var received = await context.Friendships
            .AsNoTracking()
            .Include(f => f.RequestingUser)
            .Where(f => f.ReceivingId == userId)
            .Where(f => f.Pending == false)
            .Where(f => f.RequestingUser.UserName!.Contains(model.SearchName ?? string.Empty))
            .Take(model.Limit)
            .Select(f => new FriendReturnModel
            {
                Id = f.Id,
                OtherUser = f.RequestingUser.ToReturnModel()
            })
            .ToListAsync();

        var result = requests
            .Concat(received)
            .OrderBy(model.Order!.Value, f => f.OtherUser.Name)
            .Take(model.Limit)
            .ToList();

        return result;
    }

    /// <returns>(requested, received)</returns>
    public async Task<(FriendReturnModel?, FriendReturnModel?)> SearchSingularFriendsAsync(int userId, FriendSearchModel model)
    {
        var requested = await context.Friendships
            .AsNoTracking()
            .Include(f => f.ReceivingUser)
            .Where(f => f.RequestingId == userId && f.Pending == false && f.ReceivingId == model.SearchUserId)
            .Take(1)
            .Select(f => new FriendReturnModel
            {
                Id = f.Id,
                OtherUser = f.ReceivingUser.ToReturnModel()
            })
            .FirstOrDefaultAsync();

        var received = await context.Friendships
            .AsNoTracking()
            .Include(f => f.RequestingUser)
            .Where(f => f.ReceivingId == userId && f.Pending == false && f.RequestingId == model.SearchUserId)
            .Take(1)
            .Select(f => new FriendReturnModel
            {
                Id = f.Id,
                OtherUser = f.RequestingUser.ToReturnModel()
            })
            .FirstOrDefaultAsync();

        return (requested, received);
    }

    public async Task<User> CreateUserAsync(UserCreateModel model, string passwordHash)
    {
        var colorModel = model.BannerColor;
        var color = Color.FromArgb(colorModel.Red!.Value, colorModel.Green!.Value, colorModel.Blue!.Value);

        var user = new User
        {
            UserName = model.Name,
            DisplayName = model.DisplayName,
            Email = model.Email,
            Description = model.Description,
            BannerColor = color,
            CreatedAt = DateTime.UtcNow,
            PasswordHash = passwordHash,
        };

        await manager.CreateAsync(user);

        return user;
    }

    public async Task CreateSettingsForUserAsync(int userId)
    {
        var settings = new Settings
        {
            UserId = userId,
            EnableSounds = true,
        };

        context.Settings.Add(settings);

        await context.SaveChangesAsync();
    }

    public async Task SaveUsersSettingsAsync(Settings settings)
    {
        context.Settings.Update(settings);
        await context.SaveChangesAsync();
    }

    public async Task ValidateUserAsync(User user)
    {
        user.IsActivated = true;

        await context.SaveChangesAsync();
    }

    public async Task<Friendship> RequestFriendshipAsync(int userId, FriendRequestCreateModel model)
    {
        var friendship = new Friendship
        {
            ReceivingId = model.UserId!.Value,
            RequestingId = userId,
        };

        context.Friendships.Add(friendship);

        await context.SaveChangesAsync();

        return friendship;
    }

    public async Task AcceptFriendshipAsync(Friendship friendRequest)
    {
        friendRequest.Pending = false;

        await context.SaveChangesAsync();
    }

    public async Task<UserDetailModel> UpdateUserAsync(User user, UserUpdateModel model)
    {
        var modelColor = model.BannerColor;
        var color = Color.FromArgb(modelColor.Red!.Value, modelColor.Green!.Value, modelColor.Blue!.Value);

        user.DisplayName = model.DisplayName;
        user.Description = model.Description;
        user.BannerColor = color;

        await context.SaveChangesAsync();

        var returnModel = new UserDetailModel
        {
            Id = user.Id,
            Name = user.UserName!,
            DisplayName = user.DisplayName,
            CreatedAt = user.CreatedAt,
            Description = user.Description,
            BannerColor = new ColorReturnModel
            {
                Red = user.BannerColor.R,
                Green = user.BannerColor.G,
                Blue = user.BannerColor.B
            },
            Email = user.Email!,
            IsActivated = user.IsActivated
        };

        return returnModel;
    }

    public async Task ChangeUserPasswordAsync(User user, string passwordHash)
    {
        user.PasswordHash = passwordHash;

        await context.SaveChangesAsync();
    }

    public async Task RemoveRequestedFriendshipsAsync(int userId)
    {
        await context.Friendships
            .Where(x => x.RequestingId == userId)
            .ExecuteDeleteAsync();
    }

    public async Task RemoveReceivedFriendshipsAsync(int userId)
    {
        await context.Friendships
            .Where(x => x.ReceivingId == userId)
            .ExecuteDeleteAsync();
    }

    public async Task RemoveUsersUserChatsAsync(int userId)
    {
        await context.UserChats
            .Where(x => x.UserId == userId)
            .ExecuteDeleteAsync();
    }

    public async Task RemoveUsersSettingsAsync(int userId)
    {
        await context.Settings
            .Where(s => s.UserId == userId)
            .ExecuteDeleteAsync();
    }

    public async Task RemoveUsersMessagesAsync(int userId)
    {
        await context.Messages
            .Where(x => x.ChatId != null && x.UserId == userId)
            .Include(x => x.Chat)
            .ThenInclude(x => x!.UserChats)
            .Where(x => x.Chat!.UserChats.Count == 0)
            .ExecuteDeleteAsync();
    }

    public async Task RemoveUsersUserServers(int userId)
    {
        await context.UserServers
            .Where(x => x.Id == userId)
            .ExecuteDeleteAsync();
    }

    public async Task DeleteUserAsync(int userId)
    {
        await context.Users
            .Where(x => x.Id == userId)
            .ExecuteDeleteAsync();
    }

    public async Task RemoveFriendshipAsync(int friendshipId)
    {
        await context.Friendships
            .Where(f => f.Id == friendshipId)
            .ExecuteDeleteAsync();
    }

    public async Task AssignRolesToUserAsync(User user, params string[] roles)
    {
        await manager.AddToRolesAsync(user, roles);
    }
}
