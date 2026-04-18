using System.Diagnostics;
using Cyber_Cord.App.Components;
using Cyber_Cord.App.Components.Dialogs;
using Cyber_Cord.App.Enums;
using Cyber_Cord.App.Models;
using Cyber_Cord.App.Services;
using Cyber_Cord.App.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Shared.Models;
using Shared.Types;
using Shared.Types.Interfaces;

namespace Cyber_Cord.App.Pages;

public partial class Home : ComponentBase, IDisposable
{
    private const string _operationFailedError = "Operation failed!";

    private UserModel _currentUser = default!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;
    [Inject]
    private SessionState SessionState { get; set; } = default!;
    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;
    [Inject]
    private WebSocketService WebSocketService { get; set; } = default!;
    [Inject]
    private ApiService ApiService { get; set; } = default!;

    [Inject]
    private SoundNotificationService SoundNotificationService { get; set; } = default!;

    [Inject]
    private ErrorProviderService ErrorProviderService { get; set; } = default!;

    [Inject]
    private UserSettingsService UserSettingsService { get; set; } = default!;
    

    [CascadingParameter]
    public EventCallback UpdateState { get; set; }

    // State management
    private DmViewState _currentDmView = DmViewState.Friends;
    private bool _directMessages = true;
    private bool _isAddingFriend = false;
    private int? _selectedServerId;
    private int? selectedChatId;
    private int? _selectedChannelIndex;
    private int? _selectedChatIndex;

    private List<ServerModel> _servers = [];
    private List<ChannelModel> _channels = [];
    private List<FriendModel> _friends = [];
    private List<FriendRequestModel> _friendRequests = [];
    private List<ChatModel> _chats = [];
    private readonly List<UserModel> _loadedUsers = [];

    private AutoMessagePager? _messagePager;

    private string? _lastPing = "No one loves you";
    private const string _pingMaxOpacityFunction = "pingMaxOpacity";
    private const string _pingMinOpacityFunction = "pingMinOpacity";

    // UI State
    private string SidebarTitle => _directMessages ? "Direct Messages" : CurrentServerName;

    private string _chatName = string.Empty;
    private string ContentTitle {
        get
        {
            return _currentDmView switch
            {
                DmViewState.Chats when _selectedChatIndex is not null => _chatName,
                DmViewState.Channels when _selectedChannelIndex is not null => GetChannelName(_channels[_selectedChannelIndex.Value]),
                DmViewState.Chats => "Messages",
                DmViewState.Channels => "Channels",
                DmViewState.Friends => "Friends",
                DmViewState.Requests => "Friend Requests",

                _ => "Error"
            };
        }
    }

    // Input
    private string _newMessage = string.Empty;
    private string _newFriendUsername = string.Empty;

    // Helpers
    private string CurrentServerName => _selectedServerId is not null 
        ? _servers.FirstOrDefault(s => s.Id == _selectedServerId)?.Name ?? "Server" 
        : "Server";

    protected override async Task OnInitializedAsync()
    {
        var user = await ApiService.GetCurrentUserAsync();

        if (user is not null)
        {
            var result = await SessionState.RequestSessionAsync(user.Id!.Value);

            if (!result.IsOk())
            {
                await ErrorProviderService.ShowErrorAsync(UpdateState, result.Error ?? "Could not connect to websocket");
                return;
            }
        }

        if (!SessionState.IsAuthenticated)
        {
            NavigationManager.NavigateTo("/login");
            return;
        }

        WebSocketService.ReceivedMessageAsync += WebSocketService_ReceivedMessageAsync;

        await LoadAccountAsync();

        await LoadGeneralAsync();

        if (_chats.Count > 0)
            await SelectChatByIndexAsync(0);
    }

    private async Task WebSocketService_ReceivedMessageAsync(WebsocketMessageModel model)
    {
        foreach (var message in model.Messages)
        {
            // Task.WaitAll is not support on browsers... :(
            await ReactToMessageAsync(message);
        }
    }

    private async Task ReactToMessageAsync(IWebSocketMessage message)
    {
        switch (message)
        {
            case WebSocketPingModel ping:
                await GetPing(NotificationSound.Default);
                _lastPing = ping.OriginatingUserName;
                await JsRuntime.InvokeVoidAsync(_pingMaxOpacityFunction);
                await InvokeAsync(StateHasChanged);
                await Task.Yield();
                await JsRuntime.InvokeVoidAsync(_pingMinOpacityFunction);
                await InvokeAsync(StateHasChanged);
                break;
            case WebSocketConfigurationModel configuration:
                await WebSocketConfigurationAsync(configuration);
                break;
            case WebSocketMessageActionModel action:
                await HandleMessageActionAsync(action);
                break;
            case WebSocketGeneralMessageModel general:
                await WebSocketGeneralReloadAsync(general);
                break;
            case WebSocketServerModel server:
                await WebSocketServerReloadAsync(server);
                break;
            case WebSocketCallMessageModel call:
                
                break;
        }
    }

    private async Task WebSocketConfigurationAsync(WebSocketConfigurationModel configuration)
    {
        switch (configuration.Type)
        {
            case WebSocketConfigurationModel.MessageType.Kill:
                await LogOutAsync();
                break;
            case WebSocketConfigurationModel.MessageType.Error:
                await ErrorProviderService.ShowErrorAsync(
                    UpdateState,
                    $"Encountered an error while receiving data from websocket: {configuration.Reason ?? "Unknown Error"}"
                    );
                break;
            default:
                await ErrorProviderService.ShowErrorAsync(UpdateState, "Encountered unsupported message type in websocket");
                break;
        }
    }

    private async Task HandleMessageActionAsync(WebSocketMessageActionModel message)
    {
        if (message.ChatId is not null)
        {
            await HandleChatMessageActionAsync(message);
            return;
        }
        if (message.ChannelId is not null)
        {
            await HandleChannelMessageActionAsync(message);
            return;
        }

        await ErrorProviderService.ShowErrorAsync(UpdateState, "Encountered unsupported message type in websocket");
    }

    private async Task HandleChatMessageActionAsync(WebSocketMessageActionModel message)
    {
        if (_selectedChatIndex is null || message.ChatId != _chats[_selectedChatIndex.Value].Id || _messagePager is null)
        {
            return;
        }

        switch (message.Action)
        {
            case WebSocketMessageActionModel.ActionType.Received:
                await _messagePager.ReloadAsync();
                await GetPing(NotificationSound.Join);
                break;
            case WebSocketMessageActionModel.ActionType.Removed:
                await LoadChatAsync(message.ChatId!.Value);
                break;
            case WebSocketMessageActionModel.ActionType.Altered:
                await LoadChatAsync(message.ChatId!.Value);
                break;
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task HandleChannelMessageActionAsync(WebSocketMessageActionModel message)
    {
        if (_selectedChannelIndex is null
            ||  message.ChannelId != _channels[_selectedChannelIndex.Value].Id
            ||  _messagePager is null
            ||  _currentDmView != DmViewState.Channels
        ) {
            return;
        }

        switch (message.Action)
        {
            case WebSocketMessageActionModel.ActionType.Received:
                await _messagePager.ReloadAsync();
                await GetPing(NotificationSound.Join);
                break;
            case WebSocketMessageActionModel.ActionType.Removed:
                await LoadChannelAsync(message.ChannelId!.Value);
                break;
            case WebSocketMessageActionModel.ActionType.Altered:
                await LoadChannelAsync(message.ChannelId!.Value);
                break;
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task WebSocketGeneralReloadAsync(WebSocketGeneralMessageModel general)
    {
        switch (general.Scope)
        {
            case WebSocketGeneralMessageModel.ScopeType.Server:
                await LoadServersOnlyAsync();
                break;
            case WebSocketGeneralMessageModel.ScopeType.Chat:
                await LoadChatsAsync();
                await LoadUserDialogsAsync();
                break;
            case WebSocketGeneralMessageModel.ScopeType.Friend:
                await LoadFriendsAsync();
                break;
            case WebSocketGeneralMessageModel.ScopeType.Request:
                await LoadFriendRequestsAsync();
                await SoundNotificationService.PlayNotificationAsync(NotificationSound.FriendRequest);
                break;
            case WebSocketGeneralMessageModel.ScopeType.Settings:
                await LoadOnSettingsAsync();
                break;
            default: return;
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task WebSocketServerReloadAsync(WebSocketServerModel model)
    {
        if (_selectedServerId != model.ServerId)
        {
            return;
        }

        var server = _servers.FirstOrDefault(s => s.Id == model.ServerId);
        if (server is null)
        {
            return;
        }
        
        switch (model.Scope)
        {
            case WebSocketServerModel.ScopeType.Channel:
                await LoadChannelsAsync(server);
                break;
            case WebSocketServerModel.ScopeType.User:
                await LoadServerUsersAsync(model.ServerId);
                break;
            case WebSocketServerModel.ScopeType.Update:
                await LoadServerAsync(model.ServerId);
                break;
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task LoadOnSettingsAsync()
    {
        await LoadAccountAsync();

        switch (_currentDmView)
        {
            case DmViewState.Chats:
                await LoadChatAsync(_chats[_selectedChatIndex!.Value].Id);
                return;
            case DmViewState.Channels:
                await LoadChannelAsync(_channels[_selectedChannelIndex!.Value].Id);
                return;
        }
    }

    private async Task LoadGeneralAsync()
    {
        var chats = LoadChatsAsync();
        var friends = LoadFriendsAsync();
        var friendRequests = LoadFriendRequestsAsync();
        var servers = LoadServersAsync();

        await Task.WhenAll(chats, friends, friendRequests, servers);

        await LoadUserDialogsAsync();

        await InvokeAsync(StateHasChanged);
    }

    private async Task LoadChatsAsync()
    {
        int? chatId = null;

        if (_selectedChatIndex is not null)
        {
            chatId = _chats[_selectedChatIndex.Value].Id;
        }

        var result = await ApiService.GetUsersChatsAsync();

        if (result is null || !result.IsOk())
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, $"When loading chats: {result?.Error}");
            return;
        }

        var chats = result.Value!;
        
        var tasks = new Task<string>[chats.Count];
        foreach (var (index, chat) in chats.Index())
        {
            tasks[index] = GetChatNameAsync(chat);
        }

        await Task.WhenAll(tasks);

        foreach (var (index, task) in tasks.Index())
        {
            chats[index].Name = task.Result;
        }

        _chats = chats;

        await SwitchChatStateAsync(chatId);
    }

    private async Task SwitchChatStateAsync(int? chatId)
    {
        int index = -1;

        if (chatId is not null)
        {
            index = _chats.FindIndex(x => x.Id == chatId);
        }

        if (_currentDmView == DmViewState.Chats)
        {
            if (index == -1)
            {
                index = 0;
            }

            await SelectChatByIndexAsync(index);
        }
        else if (_currentDmView == DmViewState.Friends || _currentDmView == DmViewState.Requests)
        {
            var previousDmViewState = _currentDmView;

            await ShowDirectMessagesAsync();

            _currentDmView = previousDmViewState;
        }
    }

    private async Task LoadFriendsAsync()
    {
        var result = await ApiService.GetUsersFriendsAsync();

        if (!result.IsOk())
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, $"When loading friends: {result.Error}");
            return;
        }

        _friends = result.Value!;
    }

    private async Task LoadFriendRequestsAsync()
    {
        var result = await ApiService.GetUsersFriendRequestsAsync();

        if (!result.IsOk())
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, $"When loading friend requests: {result.Error}");
            return;
        }

        _friendRequests = result.Value!;
    }

    private async Task LoadServersAsync()
    {
        await LoadServersOnlyAsync();

        if (_selectedServerId is null)
        {
            return;
        }

        var server = _servers.FirstOrDefault(x => x.Id == _selectedServerId);

        if (server is null)
        {
            var previousDmView = _currentDmView;

            await ShowDirectMessagesAsync();

            if (previousDmView != DmViewState.Channels)
            {
                _currentDmView = previousDmView;
            }

            return;
        }

        await LoadChannelsAsync(server);
    }

    private async Task LoadServersOnlyAsync()
    {
        var result = await ApiService.GetUserServersAsync();

        if (result is null)
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, "With loading servers");
            return;
        }

        _servers = result;

        if (!result.Any(s => s.Id == _selectedServerId) && _selectedServerId != null)
        {
            _currentDmView = DmViewState.Friends;
            _directMessages = true;
            _selectedServerId = null;
            _selectedChatIndex = null;
            _selectedChannelIndex = null;
        }
    }

    private async Task LoadChannelsAsync(ServerModel server)
    {
        int? channelId = null;

        if (_selectedChannelIndex is not null)
        {
            channelId = _channels[_selectedChannelIndex.Value].Id;
        }

        var channels = await ApiService.GetServerChannelsAsync(server.Id);

        if (channels is null)
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, "Could not load channels");
            return;
        }

        _channels = channels;

        if (_selectedServerId is null)
        {
            return;
        }

        if (channelId is not null)
        {
            var index = _channels.FindIndex(x => x.Id == channelId);

            if (index == -1)
            {
                index = 0;
            }

            await SelectChannelByIndexAsync(index);
        }
        else if (_currentDmView == DmViewState.Channels && _channels.Any())
        {
            await SelectChannelByIndexAsync(0);
        }
        else if (_currentDmView == DmViewState.Channels)
        {
            return;
        }
        else
        {
            var previousDmViewState = _currentDmView;

            await ShowDirectMessagesAsync();

            _currentDmView = previousDmViewState;
        }
    }

    private async Task LoadUserDialogsAsync()
    {
        var serverPeopleBorrowState = BorrowService.GetBorrowState<ServerMembersDialog, ServerMembersModel>();
        var chatPeopleBorrowState = BorrowService.GetBorrowState<ChatUsersDialog, ChatUsersModel>();

        var serverPeopleTask = serverPeopleBorrowState.ShowingConfigureDialog
            ? LoadServerMembersDialogAsync(serverPeopleBorrowState)
            : Task.CompletedTask;
        var chatPeopleTask = chatPeopleBorrowState.ShowingConfigureDialog
            ? LoadChatUsersDialogAsync(chatPeopleBorrowState)
            : Task.CompletedTask;

        await Task.WhenAll(serverPeopleTask, chatPeopleTask);
    }

    private async Task LoadServerMembersDialogAsync(BorrowState<ServerMembersDialog, ServerMembersModel> borrowState)
    {
        var model = borrowState.Model;

        borrowState.CancelDialog();

        await InvokeAsync(StateHasChanged);

        var server = _servers.FirstOrDefault(x => x.Id == model!.ServerId);

        if (server is null)
        {
            return;
        }

        await OnServerViewPeopleAsync(server);
    }

    private async Task LoadChatUsersDialogAsync(BorrowState<ChatUsersDialog, ChatUsersModel> borrowState)
    {
        var model = borrowState.Model;

        borrowState.CancelDialog();

        await InvokeAsync(StateHasChanged);

        var chat = _chats.FirstOrDefault(x => x.Id == model!.ChatId);

        if (chat is null)
        {
            return;
        }

        await OnChatViewPeopleAsync(chat);
    }

    private async Task LoadAccountAsync()
    {
        await UserSettingsService.ReloadAsync();

        var user = await ApiService.GetUserByIdAsync(SessionState.UserId!.Value);

        if (user is null)
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, "Could not get current user info");
            return;
        }

        _currentUser = user;

        await InvokeAsync(StateHasChanged);
    }

    private async Task GetPing(NotificationSound notificationSound) => await SoundNotificationService.PlayNotificationAsync(notificationSound);

    private async Task GetLatencyAsync()
    {
        var latency = await ApiService.GetLatencyAsync();
        if (latency is null)
            return;
        await JsRuntime.InvokeVoidAsync("alert", $"Latency to server: {latency.TimeToServer} ms\nLatency from server {latency.TimeFromServer} ms");
    }

    // ===== Navigation Methods =====

    private async Task ShowDirectMessagesAsync()
    {
        _directMessages = true;
        _currentDmView = DmViewState.Friends;
        _selectedServerId = null;
        _selectedChannelIndex = null;
        _selectedChatIndex = null;

        if (_chats.Count > 0)
            await SelectChatByIndexAsync(0);
    }

    private async Task SelectServerAsync(int serverId)
    {
        _directMessages = false;
        _selectedServerId = serverId;
        _selectedChatIndex = null;
        _selectedChannelIndex = null;
        _currentDmView = DmViewState.Channels;

        await LoadServerAsync(serverId);
    }

    private async Task SelectFriendAsync(int friendId)
    {
        var chat = await ApiService.GetFriendsChatAsync(friendId);

        if (chat is null)
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, "Could not find such a chat");
            return;
        }

        var index = _chats.FindIndex(x => x.Id == chat.Id);

        if (index == -1)
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, "Error selecting the correct chat");
            return;
        }

        selectedChatId = chat.Id;
        
        await SelectChatByIndexAsync(index);
    }
    private async Task SelectChatByIndexAsync(int index)
    {
        _currentDmView = DmViewState.Chats;

        if (_chats.Count == 0)
        {
            _selectedChatIndex = null;
            await InvokeAsync(StateHasChanged);
            return;
        }

        _selectedChatIndex = index;
        var chat = _chats[index];

        _chatName = await GetChatNameAsync(_chats[_selectedChatIndex.Value]);

        selectedChatId = chat.Id;
        
        await LoadChatAsync(chat.Id);
    }

    private async Task SelectChannelByIndexAsync(int index)
    {
        _currentDmView = DmViewState.Channels;

        if (_channels.Count == 0)
        {
            _selectedChannelIndex = null;
            await InvokeAsync(StateHasChanged);
            return;
        }

        _selectedChannelIndex = index;
        var channel = _channels[index];

        await LoadChannelAsync(channel.Id);
    }

    // ===== Message Sending =====

    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(_newMessage))
            return;

        var newMsg = new MessageCreateModel
        {
            Content = _newMessage,
        };

        if (_selectedChatIndex is not null)
        {
            await SendChatMessageAsync(newMsg);
        }
        else if (_selectedChannelIndex is not null)
        {
            await SendChannelMessageAsync(newMsg);
        }

        _newMessage = string.Empty;
    }

    private async Task SendChatMessageAsync(MessageCreateModel newMsg)
    {
        int chatId = _chats[_selectedChatIndex!.Value].Id;

        await ApiService.SendMessageToChatAsync(chatId, newMsg);
    }

    private async Task SendChannelMessageAsync(MessageCreateModel newMsg)
    {
        int channelId = _channels[_selectedChannelIndex!.Value].Id;

        await ApiService.SendChannelMessageAsync(_selectedServerId!.Value, channelId, newMsg);
    }

    private async Task HandleKeyPressAsync(KeyboardEventArgs e)
    {
        if (e is { Key: "Enter", ShiftKey: false })
        {
            await SendMessageAsync();
        }
    }

    private async Task LoadChatAsync(int chatId)
    {
        await LoadChatUsersAsync(chatId);

        if (_messagePager is not null)
        {
            _messagePager.SetMessageSource(
                async (cursor, take) => await ApiService.GetChatMessagesAsync(chatId, cursor, take)
                );
            await _messagePager.ReloadAsync();
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task LoadServerAsync(int serverId)
    {
        var server = _servers.FirstOrDefault(x => x.Id == serverId);

        if (server is null)
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, "Requested server could not be loaded");
            return;
        }

        await LoadServerUsersAsync(serverId);

        await LoadChannelsAsync(server);

        await SelectChannelByIndexAsync(0);
    }

    private async Task LoadChatUsersAsync(int chatId)
    {
        var result = await ApiService.GetChatUsersAsync(chatId);

        if (result is null)
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, "Could not load chat users");
            return;
        }

        _loadedUsers.Clear();
        foreach (var user in result)
        {
            _loadedUsers.Add(user);
        }
    }

    private async Task LoadChannelAsync(int channelId)
    {
        if (_messagePager is not null)
        {
            _messagePager.SetMessageSource(
                async (cursor, take) => await ApiService.GetChannelMessagesAsync(_selectedServerId!.Value, channelId, cursor, take)
                );
            await _messagePager.ReloadAsync();
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task LoadServerUsersAsync(int serverId)
    {
        var result = await ApiService.GetServerMembersAsync(serverId);

        if (result is null)
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, "Could not load server users");
            return;
        }

        _loadedUsers.Clear();
        foreach (var user in result)
        {
            _loadedUsers.Add(user);
        }

        await LoadUserDialogsAsync();
    }

    private async Task<string> GetChatNameAsync(ChatModel chat)
    {
        if (chat.Name is not null)
        {
            return chat.Name;
        }
        
        var result = await ApiService.GetChatUsersAsync(chat.Id);

        if (result is null)
            return "Unknown";
        
        result.RemoveAll(x => x.Id == SessionState.UserId);

        if (result.Count != 1)
        {
            return "Unknown";
        }

        return $"@{result.First().DisplayName}";
    }

    private string GetChannelName(ChannelModel channel) => channel.Name;

    private async Task SendFriendRequestAsync()
    {
        var userResult = await ApiService.GetUserByNameAsync(_newFriendUsername);

        if (!userResult.IsOk())
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, userResult.Error);
            return;
        }

        var user = userResult.Value!;

        var result = await ApiService.SendFriendRequestAsync(user.Id);

        if (!result.IsOk())
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, $"Error: {result.Error}");
            return;
        }

        _isAddingFriend = false;
        _newFriendUsername = string.Empty;
    }

    private void ToggleAddFriend() => _isAddingFriend = !_isAddingFriend;

    private void CancelAddFriend()
    {
        _isAddingFriend = false;
        _newFriendUsername = string.Empty;
    }
    
    private async Task RemoveFriendAsync(int friendshipId)
    {
        await ApiService.RemoveFriendshipAsync(friendshipId);
    }
    
    private void AddServer()
    {
        var model = new ServerCreateModel();

        var borrowState = BorrowService.GetBorrowState<ServerCreateDialog, ServerCreateModel>();

        borrowState.ShowDialog(() => model);

        StateHasChanged();
    }

    private async Task AddServerCallbackAsync(Dialog<ServerCreateDialog, ServerCreateModel> dialog)
    {
        var model = dialog.Model;

        var result = await ApiService.CreateServerAsync(model) is not null;

        if (!result)
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, _operationFailedError);
        }
    }
    
    private void AddChannel()
    {
        var model = new ChannelCreateModel();

        var borrowState = BorrowService.GetBorrowState<ChannelCreateDialog, ChannelCreateModel>();

        borrowState.ShowDialog(() => model);

        StateHasChanged();
    }

    private async Task AddChannelCallbackAsync(Dialog<ChannelCreateDialog, ChannelCreateModel> dialog)
    {
        var model = dialog.Model;

        var result = await ApiService.CreateChannelAsync(_selectedServerId!.Value, model) is not null;

        if (!result)
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, _operationFailedError);
        }
    }

    private void AddServerMember()
    {
        var model = new MemberFindModel();

        var borrowState = BorrowService.GetBorrowState<AddServerMemberDialog, MemberFindModel>();

        borrowState.ShowDialog(() => model);

        StateHasChanged();
    }

    private async Task AddServerMemberCallbackAsync(Dialog<AddServerMemberDialog, MemberFindModel> dialog)
    {
        var model = dialog.Model;

        var borrowState = BorrowService.GetBorrowState<AddServerMemberDialog, MemberFindModel>();

        var user = await ApiService.GetUserByNameAsync(model.MemberName ?? string.Empty);

        if (!user.IsOk())
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, "User account with this name was not found");
            borrowState.ShowDialog(() => model);
            return;
        }

        var memberModel = new MemberAddModel
        {
            MemberId = user.Value!.Id
        };

        var result = await ApiService.AddUserToServerAsync(_selectedServerId!.Value, memberModel);

        if (!result)
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, _operationFailedError);
        }
    }

    private void AddChat()
    {
        var model = new ChatCreateModel();

        var borrowState = BorrowService.GetBorrowState<ChatCreateDialog, ChatCreateModel>();

        borrowState.ShowDialog(() => model);

        StateHasChanged();
    }

    private async Task AddChatCallback(Dialog<ChatCreateDialog, ChatCreateModel> dialog)
    {
        var model = dialog.Model;

        var result = await ApiService.CreateChatAsync(model) is not null;

        if (!result)
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, _operationFailedError);
        }
    }

    private async Task OnServerViewPeopleAsync(ServerModel server)
    {
        var borrowState = BorrowService.GetBorrowState<ServerMembersDialog, ServerMembersModel>();

        var users = await ApiService.GetServerMembersAsync(server.Id);
        var banned = await ApiService.GetServerBannedUsersAsync(server.Id);

        var model = new ServerMembersModel
        {
            ServerId = server.Id,
            Name = server.Name,
            Members = users ?? [],
            Banned = banned ?? [],
            ShowUserDialog = ShowUserDialog,
            BanUserAsync = BanUserFromServerAsync,
            KickUserAsync = KickUserFromServerAsync,
            UnbanAsync = UnbanUserInServerAsync
        };

        borrowState.ShowDialog(() => model);

        await InvokeAsync(StateHasChanged);

        async Task BanUserFromServerAsync(UserModel user)
        {
            var success = await ApiService.BanUserFromServerAsync(server.Id, user.Id!.Value);

            if (!success)
            {
                await ErrorProviderService.ShowErrorAsync(UpdateState, "You are not allowed to ban this person");
            }
        }

        async Task KickUserFromServerAsync(UserModel user)
        {
            var success = await ApiService.RemoveUserFromServerAsync(server.Id, user.Id!.Value);

            if (!success)
            {
                await ErrorProviderService.ShowErrorAsync(UpdateState, "You are not allowed to kick this person");
            }
        }

        async Task UnbanUserInServerAsync(UserModel user)
        {
            var success = await ApiService.UnbanUserFromServerAsync(server.Id, user.Id!.Value);

            if (!success)
            {
                await ErrorProviderService.ShowErrorAsync(UpdateState, "You are not allowed to unban this person");
            }
        }
    }

    private void OnServerViewInfo(ServerModel server)
    {
        var borrowState = BorrowService.GetBorrowState<ServerInfoDialog, ServerModel>();

        borrowState.ShowDialog(() => server);

        StateHasChanged();
    }
    
    private void OnChannelViewInfo(ChannelModel channel)
    {
        var borrowState = BorrowService.GetBorrowState<ChannelInfoDialog, ChannelModel>();

        borrowState.ShowDialog(() => channel);

        StateHasChanged();
    }

    private async Task OnChatViewPeopleAsync(ChatModel chat)
    {
        var borrowState = BorrowService.GetBorrowState<ChatUsersDialog, ChatUsersModel>();

        var users = await ApiService.GetChatUsersAsync(chat.Id);

        var model = new ChatUsersModel
        {
            ChatId = chat.Id,
            Name = chat.Name,
            Users = users ?? [],
            ShowUserDialog = ShowUserDialog,
            KickUserAsync = async (user) => await ApiService.RemoveUserFromChatAsync(chat.Id, user.Id!.Value),
        };

        borrowState.ShowDialog(() => model);

        await InvokeAsync(StateHasChanged);
    }

    private void OnChatAddUser(ChatModel chat)
    {
        var borrowState = BorrowService.GetBorrowState<ChatAddUserDialog, ChatAddUserModel>();

        var model = new ChatAddUserModel
        {
            Id = chat.Id,
            ChatName = chat.Name
        };

        borrowState.ShowDialog(() => model);

        StateHasChanged();
    }

    private async Task AddChatUserCallbackAsync(Dialog<ChatAddUserDialog, ChatAddUserModel> dialog)
    {
        var model = dialog.Model;

        var borrowState = BorrowService.GetBorrowState<ChatAddUserDialog, ChatAddUserModel>();

        var user = await ApiService.GetUserByNameAsync(model.UserName);

        if (!user.IsOk())
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, "User account with this name was not found");
            borrowState.ShowDialog(() => model);
            return;
        }

        var userChatModel = new UserChatCreateModel
        {
            UserId = user.Value!.Id
        };

        var result = await ApiService.AddUserToChatAsync(model.Id, userChatModel);

        if (!result)
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, _operationFailedError);
        }
    }

    private async Task RemoveChatMessageAsync(MessageModel message)
    {
        var chatId = _chats[_selectedChatIndex!.Value].Id;

        var success = await ApiService.RemoveMessageFromChatAsync(chatId, message.Id);

        if (!success)
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, "Could not remove this message");
        }
    }

    private async Task RemoveChannelMessageAsync(MessageModel message)
    {
        var channelId = _channels[_selectedChannelIndex!.Value].Id;

        var success = await ApiService.RemoveMessageFromChannelAsync(_selectedServerId!.Value, channelId, message.Id);

        if (!success)
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, "Could not remove this message");
        }
    }

    private void OnMessageUpdate(MessageModel model)
    {
        var borrowState = BorrowService.GetBorrowState<MessageEditDialog, MessageUpdateModel>();

        var messageUpdateModel = new MessageUpdateModel
        {
            Id = model.Id,
            Content = model.Content
        };

        borrowState.ShowDialog(() => messageUpdateModel);

        StateHasChanged();
    }

    private async Task OnMessageUpdateCallbackAsync(Dialog<MessageEditDialog, MessageUpdateModel> dialog)
    {
        var model = dialog.Model;

        if (_selectedChatIndex is null)
        {
            await OnChannelMessageUpdateCallbackAsync(model);
        }
        else
        {
            await OnChatMessageUpdateCallbackAsync(model);
        }
    }

    private async Task OnChatMessageUpdateCallbackAsync(MessageUpdateModel model)
    {
        var chatId = _chats[_selectedChatIndex!.Value].Id;

        var result = await ApiService.UpdateChatMessageAsync(chatId, model);

        if (result is null)
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, "Could not update this message");
        }
    }

    private async Task OnChannelMessageUpdateCallbackAsync(MessageUpdateModel model)
    {
        var channelId = _channels[_selectedChannelIndex!.Value].Id;

        var result = await ApiService.UpdateChannelMessageAsync(_selectedServerId!.Value, channelId, model);

        if (result is null)
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, "Could not update this message");
        }
    }

    private async Task OnOpenSettingsAsync()
    {
        var detail = await ApiService.GetUserDetailAsync();

        if (detail is null)
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, "Getting this user's account detail failed");
            return;
        }

        var borrowState = BorrowService.GetBorrowState<UserSettingsDialog, UserSettingsDialogModel>();

        var model = new UserSettingsDialogModel
        {
            UserDetail = detail,
            LogOutAsync = LogOutAsync,
            GetLatencyAsync = GetLatencyAsync,
            DeleteAccount = DeleteAccount,
            ChangePassword = ChangePassword,
        };

        borrowState.ShowDialog(() => model);

        StateHasChanged();
    }

    private async Task OnOpenSettingsCallbackAsync(Dialog<UserSettingsDialog, UserSettingsDialogModel> dialog)
    {
        var model = dialog.Model;

        var userUpdateModel = new UserUpdateModel
        {
            Description = model.UserDetail.Description,
            DisplayName = model.UserDetail.DisplayName,
            BannerColor = new ColorCreateModel
            {
                Red = model.UserDetail.BannerColor.Red,
                Green = model.UserDetail.BannerColor.Green,
                Blue = model.UserDetail.BannerColor.Blue
            }
        };

        var result = await ApiService.UpdateUserAsync(userUpdateModel);

        if (result is null)
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, "Could not update user settings");
            return;
        }
    }

    private void OnModifyChat(ChatModel model)
    {
        var chatUpdateModel = new ChatUpdateModel
        {
            ChatId = model.Id,
            Name = model.Name
        };

        var borrowState = BorrowService.GetBorrowState<ChatUpdateDialog, ChatUpdateModel>();

        borrowState.ShowDialog(() => chatUpdateModel);

        StateHasChanged();
    }

    private async Task OnModifyChatCallbackAsync(Dialog<ChatUpdateDialog, ChatUpdateModel> dialog)
    {
        var model = dialog.Model;

        var result = await ApiService.UpdateChatAsync(model);

        if (result is null)
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, "Could not modify chat");
            return;
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task OnDeleteChatAsync(ChatModel chat)
    {
        var result = await ApiService.DeleteChatAsync(chat.Id);

        if (!result)
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, "Could not remove chat");
            return;
        }

        await InvokeAsync(StateHasChanged);
    }

    private void OnModifyServer(ServerModel model)
    {
        var serverUpdateModel = new ServerUpdateModel
        {
            ServerId = model.Id,
            Name = model.Name,
            Description = model.Description
        };

        var borrowState = BorrowService.GetBorrowState<ServerUpdateDialog, ServerUpdateModel>();

        borrowState.ShowDialog(() => serverUpdateModel);

        StateHasChanged();
    }

    private async Task OnModifyServerCallbackAsync(Dialog<ServerUpdateDialog, ServerUpdateModel> dialog)
    {
        var model = dialog.Model;

        var result = await ApiService.UpdateServerAsync(model);

        if (result is null)
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, "Could not modify server");
            return;
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task OnDeleteServerAsync(ServerModel server)
    {
        var result = await ApiService.DeleteServerAsync(server.Id);

        if (!result)
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, "Could not remove server");
            return;
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task CopyMessageToClipboardAsync(MessageModel message)
    {
        await JsRuntime.InvokeVoidAsync("copyTextToClipboard", message.Content);
    }

    private void OnModifyChannel(ChannelModel model)
    {
        var channelUpdateModel = new ChannelUpdateModel
        {
            ChannelId = model.Id,
            Name = model.Name,
            Description = model.Description
        };

        var borrowState = BorrowService.GetBorrowState<ChannelUpdateDialog, ChannelUpdateModel>();

        borrowState.ShowDialog(() => channelUpdateModel);

        StateHasChanged();
    }

    private async Task OnModifyChannelCallbackAsync(Dialog<ChannelUpdateDialog, ChannelUpdateModel> dialog)
    {
        var model = dialog.Model;

        var result = await ApiService.UpdateChannelAsync(_selectedServerId!.Value, model);

        if (result is null)
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, "Could not modify channel");
            return;
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task OnDeleteChannelAsync(ChannelModel channel)
    {
        var result = await ApiService.DeleteChannelAsync(_selectedServerId!.Value, channel.Id);

        if (!result)
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, "Could not remove channel");
            return;
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task PingAsync(int id)
    {
        await ApiService.PingAsync(id);
    }
    
    private async Task AcceptRequestAsync(int requestId)
    {
        var result = await ApiService.AcceptFriendRequestAsync(requestId);

        if (!result.IsOk())
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, $"Error {result.Error}");
            return;
        }
    }
    
    private async Task RejectRequestAsync(int requestId)
    {
        var result = await ApiService.RemoveFriendshipAsync(requestId);

        if (!result.IsOk())
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, $"Error {result.Error}");
            return;
        }
    }

    private void ShowUserDialog(UserModel user)
    {
        var borrowState = BorrowService.GetBorrowState<UserDisplayCard, UserDisplayCardModel>();

        var userDisplayCardModel = new UserDisplayCardModel
        {
            User = user,
            PingAsync = PingAsync
        };

        borrowState.ShowDialog(() => userDisplayCardModel);

        StateHasChanged();
    }

    private Func<CursorModel?, int, Task<Result<PaginatedMessages>>> GetCurrentMessageSource()
    {
        if (_selectedChatIndex is not null)
        {
            return async (cursor, take) => await ApiService.GetChatMessagesAsync(_chats[_selectedChatIndex!.Value].Id, cursor, take);
        }

        if (_selectedChannelIndex is not null)
        {
            return async (cursor, take) => await ApiService.GetChannelMessagesAsync(
                _selectedServerId!.Value, _channels[_selectedChannelIndex.Value].Id, cursor, take
                );
        }

        throw new UnreachableException("Was not able to get a message source");
    }

    private async Task LogOutAsync()
    {
        var borrowService = BorrowService.GetBorrowState<UserSettingsDialog, UserSettingsDialogModel>();

        borrowService.CancelDialog();

        StateHasChanged();

        var success = await ApiService.LogoutAsync();

        if (!success)
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, "Logout was unsucessful");
            StateHasChanged();
        }

        SessionState.CloseSession();

        NavigationManager.NavigateTo("/", true);
    }

    private void DeleteAccount()
    {
        var borrowState = BorrowService.GetBorrowState<PasswordConfirmationDialog, string>();

        borrowState.ShowDialog(() => string.Empty);

        StateHasChanged();
    }

    private void ChangePassword()
    {
        var borrowState = BorrowService.GetBorrowState<ChangePasswordDialog, UserPasswordChangeModel>();

        borrowState.ShowDialog(() => new()
        {
            NewPassword = default!,
            OldPassword = default!
        });

        StateHasChanged();
    }

    private async Task PasswordConfirmationCallbackAsync(Dialog<PasswordConfirmationDialog, string> dialog)
    {
        var password = dialog.Model;

        var result = await ApiService.DeleteUserAsync(SessionState.UserId!.Value, password);

        if (!result)
        {
            var borrowState = BorrowService.GetBorrowState<PasswordConfirmationDialog, string>();

            borrowState.ShowDialog(() => password);

            StateHasChanged();

            return;
        }

        await LogOutAsync();
    }

    private async Task ChangePasswordCallbackAsync(Dialog<ChangePasswordDialog, UserPasswordChangeModel> dialog)
    {
        var result = await ApiService.ChangeUserPasswordAsync(dialog.Model);

        if (!result)
        {
            var borrowState = BorrowService.GetBorrowState<ChangePasswordDialog, UserPasswordChangeModel>();

            borrowState.ShowDialog(() => dialog.Model);

            StateHasChanged();

            return;
        }

        StateHasChanged();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        WebSocketService.ReceivedMessageAsync -= WebSocketService_ReceivedMessageAsync;
    }

    ~Home()
    {
        Dispose(false);
    }

}
