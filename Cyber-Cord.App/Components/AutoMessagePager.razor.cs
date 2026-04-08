using Cyber_Cord.App.Models;
using Cyber_Cord.App.Services;
using Cyber_Cord.App.Types;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MessageSourceFunc = System.Func<Cyber_Cord.App.Models.CursorModel?, int, System.Threading.Tasks.Task<Shared.Types.Result<Cyber_Cord.App.Models.PaginatedMessages>>>;

namespace Cyber_Cord.App.Components;

public partial class AutoMessagePager {
    private List<MessageModel> _messages = [];
    private CursorModel? _cursorModel;
    private bool _loadingContent = false;
    private const int _requestCount = 50;
    private const string _contentScrollPositionFunction = "getScrollPosition";
    private const string _contentSetScrollTopFunction = "setScrollTop";
    private const string _contentId = "pager_content";
    private const double _clientDoScrollPercentage = 0.3;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;
    [Inject]
    private ErrorProviderService ErrorProviderService { get; set; } = default!;
    [CascadingParameter]
    private EventCallback UpdateState { get; set; }
    [Parameter, EditorRequired]
    public List<UserModel> Users { get; set; }
    [Parameter, EditorRequired]
    public Action<UserModel> ShowUserDialog { get; set; } = default!;

    /// <summary>
    /// First parameter: cursor 
    /// Second parameter: limit
    /// </summary>
    [Parameter, EditorRequired]
    public MessageSourceFunc MessageSource { get; set; } = default!;
    [Parameter]
    public string MessageTooltip { get; set; } = string.Empty;

    public async Task ReloadAsync()
    {
        var prevScrollPosition = await GetScrollDataAsync();

        var result = await MessageSource(null, _requestCount);

        if (!result.IsOk())
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, $"Could not reload messages: {result.Error}");
            return;
        }

        var paginated = result.Value!;
        var messages = paginated.Data;

        if (!messages.Any())
        {
            _loadingContent = false;
            await InvokeAsync(StateHasChanged);
            return;
        }

        var lastMessage = messages.Last();

        foreach (var message in _messages)
        {
            if (message.Id < lastMessage.Id)
            {
                messages.Add(message);
            }
        }

        _messages = messages;

        await InvokeAsync(StateHasChanged);

        var scrollPosition = await GetScrollDataAsync();

        if (scrollPosition is null)
        {
            return;
        }

        prevScrollPosition ??= scrollPosition;

        var prevScrollBottom = GetScrollBottom(prevScrollPosition);
        var scrollBottom = GetScrollBottom(scrollPosition);
        var difference = scrollBottom - prevScrollBottom;

        if (-scrollPosition.ScrollTop > prevScrollPosition.ClientHeight * _clientDoScrollPercentage)
        {
            var adjustedScrollBottom = scrollBottom + difference;
            var adjustedScrollTop = AdjustScrollTop(scrollPosition, adjustedScrollBottom);

            await SetScrollTop(adjustedScrollTop);
        }

        _loadingContent = scrollPosition.ScrollHeight > scrollPosition.ClientHeight;

        await InvokeAsync(StateHasChanged);
    }

    public void SetMessageSource(MessageSourceFunc messageSource)
    {
        _messages.Clear();
        MessageSource = messageSource;
    }

    private async Task<int> RequestMoreAsync()
    {
        var result = await MessageSource.Invoke(_cursorModel, _requestCount);

        if (!result.IsOk())
        {
            await ErrorProviderService.ShowErrorAsync(UpdateState, $"Could not load more messages: {result.Error}");
            return 0;
        }

        var paginated = result.Value!;

        if (_cursorModel is not null && _cursorModel.Id == paginated.Cursor.Id)
        {
            _loadingContent = false;
            return 0;
        }

        _cursorModel = paginated.Cursor;

        var messages = paginated.Data;
        foreach (var message in messages)
        {
            _messages.Add(message);
        }

        return messages.Count;
    }

    private async Task ScrollAsync()
    {
        var scrollPosition = await GetScrollDataAsync();

        bool atTheTop = (int)(scrollPosition!.ScrollHeight + scrollPosition.ScrollTop) == scrollPosition.ClientHeight;

        _loadingContent = scrollPosition.ScrollHeight > scrollPosition.ClientHeight;
        await InvokeAsync(StateHasChanged);

        if (!atTheTop || !_loadingContent)
        {
            return;
        }

        var newCount = await RequestMoreAsync();

        _loadingContent = _loadingContent && newCount > 0;

        await InvokeAsync(StateHasChanged);
    }

    private async Task<ScrollData?> GetScrollDataAsync()
    {
        try
        {
            var scrollData = await JsRuntime.InvokeAsync<ScrollData>(_contentScrollPositionFunction, _contentId);

            return scrollData;
        }
        catch (JSException)
        {
            return null;
        }
    }

    private async Task SetScrollTop(double scrollTop)
    {
        try
        {
            await JsRuntime.InvokeVoidAsync(_contentSetScrollTopFunction, _contentId, scrollTop);
        }
        catch (JSException) { } 
    }

    private double GetScrollBottom(ScrollData scrollData)
    {
        return scrollData.ScrollHeight - scrollData.ScrollTop - scrollData.ClientHeight;
    }

    private double AdjustScrollTop(ScrollData scrollData, double scrollBottom)
    {
        return scrollData.ScrollHeight - scrollData.ClientHeight - scrollBottom;
    }

    private string GetUserDisplayName(int? userId)
    {
        if (userId is null)
        {
            return "Unknown User";
        }

        var user = Users.FirstOrDefault(x => x.Id == userId);

        if (user is null)
        {
            return $"User {userId}";
        }

        return user.DisplayName;
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        await ReloadAsync();
    }
}