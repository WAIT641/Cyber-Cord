using System.Net.Http.Json;
using System.Text.Json;
using Cyber_Cord.App.Exceptions;
using Cyber_Cord.App.Models;
using Cyber_Cord.App.Options;
using Cyber_Cord.App.Shared;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Shared.Types;
using Microsoft.AspNetCore.Mvc;
using Shared;
using System.Net.Mime;
using Microsoft.AspNetCore.JsonPatch;
using Shared.Models;
using Shared.Models.ApiModels;

namespace Cyber_Cord.App.Services;

public class ApiService(IHttpClientFactory httpClientFactory, SessionState sessionState, IOptions<RouteOptions> options)
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const int _defaultMessageCount = 10;

    private readonly string _baseRoute = options.Value.BaseApiRoute
                                         ?? throw new ApiServiceException("Could not get api route in this app instance");

    // ===== Core Helper =====

    // Reads ProblemDetails from the response and returns the most descriptive
    // message available, falling back to the HTTP reason phrase if needed.
    private async Task<string> GetErrorMessageAsync(HttpResponseMessage response)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(_jsonOptions);
            if (problem?.Detail is not null) return problem.Detail;
            if (problem?.Title is not null) return problem.Title;
        }
        catch { /* body wasn't ProblemDetails */ }

        return response.ReasonPhrase ?? $"Request failed with status {(int)response.StatusCode}";
    }

    private async Task<T?> GetAsync<T>(string path) where T : class
    {
        var client = httpClientFactory.CreateClient();
        var response = await client.GetAsync(path);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<T>();
    }

    // ===== OAuth ====

    public string GetGoogleLoginUrl() => $"{_baseRoute}/auth/google-login";

    // ===== User Methods =====

    public async Task<Result<UserShortModel>> GetUserByNameAsync(string name)
    {
        var client = httpClientFactory.CreateClient();

        var queryParams = new Dictionary<string, string?>
        {
            { nameof(UserSearchModel.SingleResultOnly), true.ToString() },
            { nameof(UserSearchModel.SearchName), name }
        };

        var url = QueryHelpers.AddQueryString($"{_baseRoute}/users", queryParams);
        var response = await client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return Result<UserShortModel>.Bad(await GetErrorMessageAsync(response));

        var result = await response.Content.ReadFromJsonAsync<List<UserShortModel>>(_jsonOptions);

        if (result is null || result.Count != 1)
            return Result<UserShortModel>.Bad("User not found.");

        return Result<UserShortModel>.Ok(result.First());
    }

    public async Task<UserModel?> GetUserByIdAsync(int id)
    {
        return await GetAsync<UserModel>($"{_baseRoute}/users/{id}");
    }

    public async Task<UserModel?> GetCurrentUserAsync()
    {
        var user = await GetAsync<UserModel>($"{_baseRoute}/users/me");
        return user;
    }
    
    public async Task<UserDetailModel?> GetUserDetailAsync()
    {
        return await GetAsync<UserDetailModel>($"{_baseRoute}/users/{sessionState.UserId}/detail");
    }

    public async Task<SettingsModel?> GetUserSettingsAsync()
    {
        return await GetAsync<SettingsModel>($"{_baseRoute}/users/{sessionState.UserId}/settings");
    }

    public async Task<Result<List<ChatModel>>> GetUsersChatsAsync()
    {
        var client = httpClientFactory.CreateClient();
        var response = await client.GetAsync($"{_baseRoute}/chats");

        if (!response.IsSuccessStatusCode)
            return Result<List<ChatModel>>.Bad(await GetErrorMessageAsync(response));

        var chats = await response.Content.ReadFromJsonAsync<List<ChatModel>>(_jsonOptions);

        return chats is null
            ? Result<List<ChatModel>>.Bad("Response could not be parsed.")
            : Result<List<ChatModel>>.Ok(chats);
    }

    public async Task<Result<List<FriendModel>>> GetUsersFriendsAsync()
    {
        var client = httpClientFactory.CreateClient();
        var response = await client.GetAsync($"{_baseRoute}/users/{sessionState.UserId}/friends");

        if (!response.IsSuccessStatusCode)
            return Result<List<FriendModel>>.Bad(await GetErrorMessageAsync(response));

        var friends = await response.Content.ReadFromJsonAsync<List<FriendModel>>(_jsonOptions);

        return friends is null
            ? Result<List<FriendModel>>.Bad("Response could not be parsed.")
            : Result<List<FriendModel>>.Ok(friends);
    }

    public async Task<Result> IsFriendWithAsync(int foreignUserId)
    {
        var queryParams = new Dictionary<string, string?>
        {
            { nameof(FriendGetSingleModel.SearchUserId), foreignUserId.ToString() },
        };

        var url = QueryHelpers.AddQueryString($"{_baseRoute}/users/{sessionState.UserId}/friends", queryParams);
        var client = httpClientFactory.CreateClient();
        var response = await client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return Result.Bad(await GetErrorMessageAsync(response));

        return Result.Ok();
    }

    public async Task<Result<List<FriendRequestModel>>> GetUsersFriendRequestsAsync()
    {
        var client = httpClientFactory.CreateClient();
        var response = await client.GetAsync($"{_baseRoute}/users/{sessionState.UserId}/pending");

        if (!response.IsSuccessStatusCode)
            return Result<List<FriendRequestModel>>.Bad(await GetErrorMessageAsync(response));

        var friendRequests = await response.Content.ReadFromJsonAsync<List<FriendRequestModel>>(_jsonOptions);

        return friendRequests is null
            ? Result<List<FriendRequestModel>>.Bad("Response could not be parsed.")
            : Result<List<FriendRequestModel>>.Ok(friendRequests);
    }

    public async Task<ChatModel?> GetFriendsChatAsync(int friendshipId)
    {
        return await GetAsync<ChatModel>($"{_baseRoute}/users/{sessionState.UserId}/friends/{friendshipId}/chat");
    }

    public async Task<Result<UserModel>> CreateUserAsync(UserCreateModel user)
    {
        var client = httpClientFactory.CreateClient();
        var response = await client.PostAsJsonAsync($"{_baseRoute}/users", user, _jsonOptions);

        if (!response.IsSuccessStatusCode)
            return Result<UserModel>.Bad(await GetErrorMessageAsync(response));

        var result = await response.Content.ReadFromJsonAsync<UserModel>(_jsonOptions);

        return result is null
            ? Result<UserModel>.Bad("Response could not be parsed.")
            : Result<UserModel>.Ok(result);
    }
    public async Task<bool> ActivateUserAsync(int id, string validationToken)
    {
        var client = httpClientFactory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseRoute}/users/{id}/activate");
        request.Headers.Add(Headers.ValidationTokenHeader, validationToken);

        var response = await client.SendAsync(request);

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ResendValidationCodeAsync(int id, string password)
    {
        var client = httpClientFactory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseRoute}/users/{id}/resendcode");
        request.Headers.Add(Headers.UserPasswordHeader, password);

        var response = await client.SendAsync(request);

        return response.IsSuccessStatusCode;
    }

    public async Task<Result> SendFriendRequestAsync(int userId)
    {
        var client = httpClientFactory.CreateClient();

        var friendRequest = new FriendRequestCreateModel { UserId = userId };
        int currentUserId = sessionState.UserId!.Value;

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseRoute}/users/{currentUserId}/friends")
        {
            Content = JsonContent.Create(friendRequest, options: _jsonOptions)
        };

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return Result.Bad(await GetErrorMessageAsync(response));

        return Result.Ok();
    }

    public async Task<Result> AcceptFriendRequestAsync(int friendRequestId)
    {
        var client = httpClientFactory.CreateClient();
        int userId = sessionState.UserId!.Value;

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseRoute}/users/{userId}/pending/{friendRequestId}/accept");
        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return Result.Bad(await GetErrorMessageAsync(response));

        return Result.Ok();
    }

    public async Task<UserDetailModel?> UpdateUserAsync(UserUpdateModel user)
    {
        var client = httpClientFactory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Put, $"{_baseRoute}/users/{sessionState.UserId}")
        {
            Content = JsonContent.Create(user, options: _jsonOptions)
        };

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<UserDetailModel>(_jsonOptions);
    }

    public async Task<bool> ChangeUserPasswordAsync(UserPasswordChangeModel model)
    {
        var client = httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Put, $"{_baseRoute}/users/{sessionState.UserId}/password")
        {
            Content = JsonContent.Create(model, options: _jsonOptions)
        };

        var response = await client.SendAsync(request);

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ReplacePatchSettingsAsync<T>(System.Linq.Expressions.Expression<Func<SettingsModel, T>> selector, T value)
    {
        using var client = httpClientFactory.CreateClient();
        var patchDoc = new JsonPatchDocument<SettingsModel>();

        var userId = sessionState.UserId;

        patchDoc.Replace(selector, value);

        var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(patchDoc);

        var content = new StringContent(
            serialized,
            mediaType: new (MediaTypeNames.Application.JsonPatch)
            );

        var response = await client.PatchAsync($"{_baseRoute}/users/{userId}/settings", content);

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteUserAsync(int id, string password)
    {
        var client = httpClientFactory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Delete, $"{_baseRoute}/users/{id}");
        request.Headers.Add(Headers.UserPasswordHeader, password);

        var response = await client.SendAsync(request);

        return response.IsSuccessStatusCode;
    }

    public async Task<Result> RemoveFriendshipAsync(int friendshipId)
    {
        var client = httpClientFactory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Delete, $"{_baseRoute}/users/{sessionState.UserId}/friends/{friendshipId}");
        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return Result.Bad(await GetErrorMessageAsync(response));

        return Result.Ok();
    }

    public async Task PingAsync(int id)
    {
        var client = httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseRoute}/users/{id}/ping");
        _ = await client.SendAsync(request);
    }

    // ===== Server Methods =====

    public async Task<List<ServerModel>?> GetUserServersAsync()
    {
        return await GetAsync<List<ServerModel>>($"{_baseRoute}/servers");
    }

    public async Task<ServerModel?> CreateServerAsync(ServerCreateModel model)
    {
        var client = httpClientFactory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseRoute}/servers")
        {
            Content = JsonContent.Create(model, options: _jsonOptions)
        };

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<ServerModel>(_jsonOptions);
    }

    public async Task<ServerModel?> UpdateServerAsync(ServerUpdateModel model)
    {
        var client = httpClientFactory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Put, $"{_baseRoute}/servers/{model.ServerId}")
        {
            Content = JsonContent.Create(model, options: _jsonOptions)
        };

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<ServerModel>(_jsonOptions);
    }

    public async Task<bool> DeleteServerAsync(int id)
    {
        var client = httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{_baseRoute}/servers/{id}");
        var response = await client.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<UserModel>?> GetServerMembersAsync(int serverId)
    {
        return await GetAsync<List<UserModel>>($"{_baseRoute}/servers/{serverId}/members");
    }

    public async Task<List<UserModel>?> GetServerBannedUsersAsync(int serverId)
    {
        return await GetAsync<List<UserModel>>($"{_baseRoute}/servers/{serverId}/bans");
    }

    public async Task<bool> AddUserToServerAsync(int serverId, MemberAddModel model)
    {
        var client = httpClientFactory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseRoute}/servers/{serverId}/members")
        {
            Content = JsonContent.Create(model, options: _jsonOptions)
        };

        var response = await client.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RemoveUserFromServerAsync(int serverId, int memberId)
    {
        var client = httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{_baseRoute}/servers/{serverId}/members/{memberId}");
        var response = await client.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> BanUserFromServerAsync(int serverId, int userId)
    {
        var client = httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseRoute}/servers/{serverId}/bans/{userId}");
        var response = await client.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UnbanUserFromServerAsync(int serverId, int userId)
    {
        var client = httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{_baseRoute}/servers/{serverId}/bans/{userId}");
        var response = await client.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    // ===== Chat Methods =====

    public async Task<List<UserModel>?> GetChatUsersAsync(int chatId)
    {
        return await GetAsync<List<UserModel>>($"{_baseRoute}/chats/{chatId}/users");
    }

    public async Task<Result<PaginatedMessages>> GetChatMessagesAsync(int chatId, CursorModel? cursor, int count = _defaultMessageCount)
    {
        var queryParams = new Dictionary<string, string?>
        {
            { "PageSize", count.ToString() }
        };

        if (cursor is not null)
        {
            queryParams.Add("Cursor.Time", cursor.Time.ToString("O"));
            queryParams.Add("Cursor.Id", cursor.Id.ToString());
        }

        var url = QueryHelpers.AddQueryString($"{_baseRoute}/chats/{chatId}/messages", queryParams);
        var client = httpClientFactory.CreateClient();
        var response = await client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            return Result<PaginatedMessages>.Bad(await GetErrorMessageAsync(response));
        }

        var result = await response.Content.ReadFromJsonAsync<PaginatedMessages>(_jsonOptions);

        return result is null
            ? Result<PaginatedMessages>.Bad("Response could not be parsed.")
            : Result<PaginatedMessages>.Ok(result);
    }

    public async Task<ChatModel?> CreateChatAsync(ChatCreateModel model)
    {
        var client = httpClientFactory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseRoute}/chats")
        {
            Content = JsonContent.Create(model, options: _jsonOptions)
        };

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<ChatModel>(_jsonOptions);
    }

    public async Task<bool> AddUserToChatAsync(int chatId, UserChatCreateModel model)
    {
        var client = httpClientFactory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseRoute}/chats/{chatId}/users")
        {
            Content = JsonContent.Create(model, options: _jsonOptions)
        };

        var response = await client.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> SendMessageToChatAsync(int chatId, MessageCreateModel model)
    {
        var client = httpClientFactory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseRoute}/chats/{chatId}/messages")
        {
            Content = JsonContent.Create(model, options: _jsonOptions)
        };

        var response = await client.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<ChatModel?> UpdateChatAsync(ChatUpdateModel model)
    {
        var client = httpClientFactory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Put, $"{_baseRoute}/chats/{model.ChatId}")
        {
            Content = JsonContent.Create(model, options: _jsonOptions)
        };

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<ChatModel>(_jsonOptions);
    }

    public async Task<MessageModel?> UpdateChatMessageAsync(int id, MessageUpdateModel model)
    {
        var client = httpClientFactory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Put, $"{_baseRoute}/chats/{id}/messages/{model.Id}")
        {
            Content = JsonContent.Create(model, options: _jsonOptions)
        };

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<MessageModel>(_jsonOptions);
    }

    public async Task<bool> DeleteChatAsync(int id)
    {
        var client = httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{_baseRoute}/chats/{id}");
        var response = await client.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RemoveUserFromChatAsync(int chatId, int userId)
    {
        var client = httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{_baseRoute}/chats/{chatId}/users/{userId}");
        var response = await client.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RemoveMessageFromChatAsync(int chatId, int messageId)
    {
        var client = httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{_baseRoute}/chats/{chatId}/messages/{messageId}");
        var response = await client.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    // ===== Channel Methods =====

    public async Task<List<ChannelModel>?> GetServerChannelsAsync(int serverId)
    {
        return await GetAsync<List<ChannelModel>>($"{_baseRoute}/servers/{serverId}/channels");
    }

    public async Task<Result<PaginatedMessages>> GetChannelMessagesAsync(int serverId, int channelId, CursorModel? cursor, int count = _defaultMessageCount)
    {
        var queryParams = new Dictionary<string, string?>
        {
            { "PageSize", count.ToString() }
        };

        if (cursor is not null)
        {
            queryParams.Add("Cursor.Time", cursor.Time.ToString("O"));
            queryParams.Add("Cursor.Id", cursor.Id.ToString());
        }

        var url = QueryHelpers.AddQueryString($"{_baseRoute}/servers/{serverId}/channels/{channelId}/messages", queryParams);
        var client = httpClientFactory.CreateClient();
        var response = await client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return Result<PaginatedMessages>.Bad(await GetErrorMessageAsync(response));

        var messages = await response.Content.ReadFromJsonAsync<PaginatedMessages>(_jsonOptions);

        return messages is null
            ? Result<PaginatedMessages>.Bad("Response could not be parsed.")
            : Result<PaginatedMessages>.Ok(messages);
    }

    public async Task<ChannelModel?> CreateChannelAsync(int serverId, ChannelCreateModel model)
    {
        var client = httpClientFactory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseRoute}/servers/{serverId}/channels")
        {
            Content = JsonContent.Create(model, options: _jsonOptions)
        };

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<ChannelModel>(_jsonOptions);
    }

    public async Task<ChannelModel?> UpdateChannelAsync(int serverId, ChannelUpdateModel model)
    {
        var client = httpClientFactory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Put, $"{_baseRoute}/servers/{serverId}/channels/{model.ChannelId}")
        {
            Content = JsonContent.Create(model, options: _jsonOptions)
        };

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<ChannelModel>(_jsonOptions);
    }

    public async Task<MessageModel?> UpdateChannelMessageAsync(int serverId, int channelId, MessageUpdateModel model)
    {
        var client = httpClientFactory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Put, $"{_baseRoute}/servers/{serverId}/channels/{channelId}/messages/{model.Id}")
        {
            Content = JsonContent.Create(model, options: _jsonOptions)
        };

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<MessageModel>(_jsonOptions);
    }

    public async Task<bool> DeleteChannelAsync(int serverId, int channelId)
    {
        var client = httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{_baseRoute}/servers/{serverId}/channels/{channelId}");
        var response = await client.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<MessageModel?> SendChannelMessageAsync(int serverId, int channelId, MessageCreateModel model)
    {
        var client = httpClientFactory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseRoute}/servers/{serverId}/channels/{channelId}/messages")
        {
            Content = JsonContent.Create(model, options: _jsonOptions)
        };

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<MessageModel>(_jsonOptions);
    }

    public async Task<bool> RemoveMessageFromChannelAsync(int serverId, int channelId, int messageId)
    {
        var client = httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{_baseRoute}/servers/{serverId}/channels/{channelId}/messages/{messageId}");
        var response = await client.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    // ===== Management Methods =====

    public async Task<LatencyModel?> GetLatencyAsync()
    {
        var client = httpClientFactory.CreateClient();

        var queryParams = new Dictionary<string, string?>
        {
            { Headers.LatencyHeader, DateTime.UtcNow.Ticks.ToString() }
        };

        var url = QueryHelpers.AddQueryString($"{_baseRoute}/management/latency", queryParams);
        var response = await client.GetAsync(url);

        var model = await response.Content.ReadFromJsonAsync<LatencyRecieveModel>(_jsonOptions);

        if (!response.IsSuccessStatusCode || model is null)
            return null;

        return new LatencyModel
        {
            TimeToServer = (model.ServerReceivedTimestamp - model.ClientTimestamp) / 10000,
            TimeFromServer = (DateTime.UtcNow.Ticks - model.ServerSentTimestamp) / 10000
        };
    }

    public async Task<bool> LoginAsync(string name, string password)
    {
        var client = httpClientFactory.CreateClient();

        var model = new LoginModel
        {
            UserName = name,
            Password = password
        };

        var result = await client.PostAsJsonAsync($"{_baseRoute}/auth/login", model, _jsonOptions);
        return result.IsSuccessStatusCode;
    }

    public async Task<bool> LogoutAsync()
    {
        var client = httpClientFactory.CreateClient();

        var result = await client.PostAsJsonAsync($"{_baseRoute}/auth/logout", _jsonOptions);

        return result.IsSuccessStatusCode;
    }

    public async Task<string?> GetWebSocketCodeAsync()
    {
        var client = httpClientFactory.CreateClient();
        var result = await client.PostAsync($"{_baseRoute}/auth/ws-code", null);

        if (!result.IsSuccessStatusCode)
        {
            return null;
        }

        var model = await result.Content.ReadFromJsonAsync<WebSocketCodeModel>();

        if (model is null)
        {
            return null;
        }

        return model.Code;
    }

    public async Task StartChatCall(int chatId, string Sdp)
    {
        var client = httpClientFactory.CreateClient();

        var message = new CallMessageModel()
        {
            Type = CallMessageModel.MessageType.Start,
            Sdp = Sdp
        };

        _ = await client.PostAsJsonAsync($"{_baseRoute}/chats/{chatId}/call", message);
    }
    
    public async Task RejectChatCall(int chatId)
    {
        var client = httpClientFactory.CreateClient();

        var message = new CallMessageModel()
        {
            Type = CallMessageModel.MessageType.Reject,
        };
        
        _ = await client.PostAsJsonAsync($"{_baseRoute}/chats/{chatId}/call", message);
    }
    
    public async Task AcceptCall(int chatId, string Sdp)
    {
        var client = httpClientFactory.CreateClient();

        var message = new CallMessageModel()
        {
            Type = CallMessageModel.MessageType.Accept,
            Sdp = Sdp
        };

        _ = await client.PostAsJsonAsync($"{_baseRoute}/chats/{chatId}/call", message);
    }
    
    public async Task EndCall(int chatId)
    {
        var client = httpClientFactory.CreateClient();

        var message = new CallMessageModel()
        {
            Type = CallMessageModel.MessageType.End,
        };

        _ = await client.PostAsJsonAsync($"{_baseRoute}/chats/{chatId}/call", message);
    }
}