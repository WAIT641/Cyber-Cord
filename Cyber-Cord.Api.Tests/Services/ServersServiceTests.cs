using Cyber_Cord.Api.Exceptions;
using Cyber_Cord.Api.Models;
using Cyber_Cord.Api.Models.Base;
using Cyber_Cord.Api.Services;
using Cyber_Cord.Api.Tests.Stubs;
using Hangfire;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shared.Enums;

namespace Cyber_Cord.Api.Tests.Services;

public class ServersServiceTests
{
    private readonly StubServersRepository _serversRepository;
    private readonly StubUserRepository _userRepository;
    private readonly ICurrentUserContext _userContext;
    private readonly ServersService _serversService;

    public ServersServiceTests()
    {
        _userRepository = new StubUserRepository();
        _userContext = Substitute.For<ICurrentUserContext>();
        _userContext.GetId().Returns(_userRepository.User1.Id);
        var serviceProvider = Substitute.For<IServiceProvider>();
        var job = Substitute.For<IBackgroundJobClient>();
        var logger = Substitute.For<ILogger<WebSocketHandler>>();
        var webSocketHandler = new WebSocketHandler(serviceProvider, job, logger);
        _serversRepository = new StubServersRepository(_userRepository);
        _serversService = new ServersService(_serversRepository, webSocketHandler, _userContext, _userRepository);
    }
    
    [Fact]
    public async Task EnsureServerAccess_ExpectsNoException()
    {
        var exception = await Record.ExceptionAsync(() =>
            _serversService.EnsureServerAccessAsync(_serversRepository.Server1.Id));
        Assert.Null(exception);
    }


    [Fact]
    public async Task EnsureServerOwner_WhenOwner_ExpectsNoException()
    {
        // User1 is the owner of Server1
        var exception = await Record.ExceptionAsync(() =>
            _serversService.EnsureServerOwnerAsync(_serversRepository.Server1.Id));
        Assert.Null(exception);
    }

    [Fact]
    public async Task EnsureServerOwner_WhenNotOwner_ThrowsForbiddenException()
    {
        // User2 is the owner of Server2, current user is User1
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _serversService.EnsureServerOwnerAsync(_serversRepository.Server2.Id));
    }
    
    [Fact]
    public async Task SearchServers_ReturnsServers()
    {
        var result = await _serversService.SearchServersAsync("Server", 2, Ordering.Asc);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, s => s.Id == _serversRepository.Server1.Id);
        Assert.Contains(result, s => s.Id == _serversRepository.Server2.Id);
    }
    
    [Fact]
    public async Task GetAllServers_ReturnsServersForCurrentUser()
    {
        var result = await _serversService.GetAllServersAsync();
        Assert.Equal(2, result.Count);
        Assert.All(result, s => Assert.NotNull(s.Name));
    }
    
    [Fact]
    public async Task GetServerById_WhenExists_ReturnsServer()
    {
        var result = await _serversService.GetServerByIdAsync(_serversRepository.Server1.Id);
        Assert.Equal(_serversRepository.Server1.Id, result.Id);
        Assert.Equal(_serversRepository.Server1.Name, result.Name);
    }
    
    [Fact]
    public async Task CreateServer_ReturnsCreatedServer()
    {
        var result = await _serversService.CreateServerAsync("New Server", "A description");
        Assert.NotNull(result);
        Assert.Equal(_serversRepository.Server1.Id, result.Id); // stub returns Server1
    }
    
    [Fact]
    public async Task UpdateServer_UpdatesNameAndDescription()
    {
        var result = await _serversService.UpdateServerAsync(
            _serversRepository.Server1.Id, "Updated Name", "Updated Desc");

        Assert.Equal("Updated Name", result.Name);
        Assert.Equal("Updated Desc", result.Description);
    }
    
    [Fact]
    public async Task DeleteServer_WhenOwner_ExpectsNoException()
    {
        var exception = await Record.ExceptionAsync(() =>
            _serversService.DeleteServerAsync(_serversRepository.Server1.Id));
        Assert.Null(exception);
    }

    [Fact]
    public async Task DeleteServer_WhenNotOwner_ThrowsForbiddenException()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _serversService.DeleteServerAsync(_serversRepository.Server2.Id));
    }
    
    [Fact]
    public async Task GetServerMembers_ReturnsMembers()
    {
        var result = await _serversService.GetServerMembersAsync(_serversRepository.Server1.Id);
        Assert.NotEmpty(result);
        Assert.Contains(result, m => m.Id == _userRepository.User1.Id);
    }
    
    [Fact]
    public async Task AddUserToServer_WhenAlreadyMember_ThrowsAlreadyExistsException()
    {
        // Stub always returns IsUserMember = true and IsUserBanned = false for User1
        await Assert.ThrowsAsync<AlreadyExistsException>(() =>
            _serversService.AddUserToServerAsync(
                _serversRepository.Server1.Id, _userRepository.User1.Id));
    }

    [Fact]
    public async Task AddUserToServer_WhenBanned_ThrowsForbiddenException()
    {
        // Stub returns IsUserBanned = true for User2 (BanUserServer1.UserId)
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _serversService.AddUserToServerAsync(
                _serversRepository.Server1.Id, _serversRepository.BanUserServer1.UserId));
    }
    
    [Fact]
    public async Task RemoveUserFromServer_WhenRemovingSelf_ThrowsBadRequestException()
    {
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _serversService.RemoveUserFromServerAsync(
                _serversRepository.Server1.Id, _userRepository.User1.Id));
    }

    [Fact]
    public async Task RemoveUserFromServer_WhenNotOwner_ThrowsForbiddenException()
    {
        // Server2's owner is User2, current user is User1
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _serversService.RemoveUserFromServerAsync(
                _serversRepository.Server2.Id, _userRepository.User2.Id));
    }
    
    [Fact]
    public async Task TransferOwnership_WhenTransferringToSelf_ThrowsBadRequestException()
    {
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _serversService.TransferServerOwnershipAsync(
                _serversRepository.Server1.Id, _userRepository.User1.Id));
    }

    [Fact]
    public async Task TransferOwnership_WhenNotOwner_ThrowsForbiddenException()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _serversService.TransferServerOwnershipAsync(
                _serversRepository.Server2.Id, _userRepository.User1.Id));
    }
    
    [Fact]
    public async Task GetBannedUsers_ReturnsBannedUsers()
    {
        var result = await _serversService.GetServerBannedUsersAsync(_serversRepository.Server1.Id);
        Assert.NotEmpty(result);
        Assert.Contains(result, u => u.Id == _userRepository.User2.Id);
    }
    
    [Fact]
    public async Task BanUser_WhenBanningSelf_ThrowsBadRequestException()
    {
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _serversService.BanUserFromServerAsync(
                _serversRepository.Server1.Id, _userRepository.User1.Id));
    }

    [Fact]
    public async Task BanUser_WhenAlreadyBanned_ThrowsBadRequestException()
    {
        // Stub returns IsUserBanned = true for User2
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _serversService.BanUserFromServerAsync(
                _serversRepository.Server1.Id, _serversRepository.BanUserServer1.UserId));
    }
    
    [Fact]
    public async Task UnbanUser_WhenOwner_ExpectsNoException()
    {
        var exception = await Record.ExceptionAsync(() =>
            _serversService.UnbanUserFromServerAsync(
                _serversRepository.Server1.Id, _serversRepository.BanUserServer1.UserId));
        Assert.Null(exception);
    }
    
    [Fact]
    public async Task GetServerChannels_ReturnsChannels()
    {
        var result = await _serversService.GetServerChannelsAsync(_serversRepository.Server1.Id);
        Assert.NotEmpty(result);
        Assert.Contains(result, c => c.Id == _serversRepository.Channel1.Id);
    }
    
    [Fact]
    public async Task GetChannelById_WhenExists_ReturnsChannel()
    {
        var result = await _serversService.GetChannelByIdAsync(
            _serversRepository.Channel1.Id, _serversRepository.Server1.Id);
        Assert.Equal(_serversRepository.Channel1.Id, result.Id);
    }
    
    [Fact]
    public async Task CreateChannel_WhenOwner_ReturnsChannel()
    {
        var model = new ChannelCreateModel { Name = "NewChannel", Description = "Desc" };
        var result = await _serversService.CreateChannelAsync(_serversRepository.Server1.Id, model);
        Assert.NotNull(result);
        Assert.Equal(_serversRepository.Channel1.Id, result.Id); // stub returns Channel1
    }

    [Fact]
    public async Task CreateChannel_WhenNotOwner_ThrowsForbiddenException()
    {
        var model = new ChannelCreateModel { Name = "NewChannel" };
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _serversService.CreateChannelAsync(_serversRepository.Server2.Id, model));
    }
    
    [Fact]
    public async Task UpdateChannel_WhenOwner_ReturnsUpdatedChannel()
    {
        var model = new ChannelUpdateModel { Name = "Renamed", Description = "New desc" };
        var result = await _serversService.UpdateChannelAsync(
            _serversRepository.Channel1.Id, _serversRepository.Server1.Id, model);
        Assert.Equal("Renamed", result.Name);
        Assert.Equal("New desc", result.Description);
    }
    
    [Fact]
    public async Task DeleteChannel_WhenOwner_ExpectsNoException()
    {
        var exception = await Record.ExceptionAsync(() =>
            _serversService.DeleteChannelAsync(
                _serversRepository.Channel1.Id, _serversRepository.Server1.Id));
        Assert.Null(exception);
    }
    
    [Fact]
    public async Task GetChannelMessages_ReturnsMessages()
    {
        var filter = new CursorPaginationFilter();
        var result = await _serversService.GetChannelMessagesAsync(
            _serversRepository.Channel1.Id, _serversRepository.Server1.Id, filter);
        Assert.NotNull(result);
        Assert.NotEmpty(result.Data);
    }
    
    [Fact]
    public async Task SendMessage_WhenChannelExists_ReturnsMessage()
    {
        var model = new MessageCreateModel { Content = "Hello!" };
        var result = await _serversService.SendMessageAsync(
            _serversRepository.Channel1.Id, _serversRepository.Server1.Id, model);
        Assert.NotNull(result);
        Assert.Equal(_serversRepository.Message1.Id, result.Id);
    }
    
    [Fact]
    public async Task UpdateChannelMessage_WhenAuthor_ReturnsUpdatedMessage()
    {
        var model = new MessageUpdateModel { Content = "Edited content" };
        var result = await _serversService.UpdateChannelMessageAsync(
            _serversRepository.Server1.Id,
            _serversRepository.Channel1.Id,
            _serversRepository.Message1.Id,
            model);
        Assert.Equal("Edited content", result.Content);
    }

    [Fact]
    public async Task UpdateChannelMessage_WhenNotAuthor_ThrowsForbiddenException()
    {
        // Switch current user to User2 who does not own Message1
        _userContext.GetId().Returns(_userRepository.User2.Id);

        var model = new MessageUpdateModel { Content = "Hacked" };
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _serversService.UpdateChannelMessageAsync(
                _serversRepository.Server1.Id,
                _serversRepository.Channel1.Id,
                _serversRepository.Message1.Id,
                model));
    }
    
    [Fact]
    public async Task DeleteChannelMessage_WhenAuthor_ExpectsNoException()
    {
        var exception = await Record.ExceptionAsync(() =>
            _serversService.DeleteChannelMessageAsync(
                _serversRepository.Server1.Id,
                _serversRepository.Channel1.Id,
                _serversRepository.Message1.Id));
        Assert.Null(exception);
    }

    [Fact]
    public async Task DeleteChannelMessage_WhenNotAuthorOrOwner_ThrowsForbiddenException()
    {
        // User2 is neither the message author (User1) nor Server1 owner (User1)
        _userContext.GetId().Returns(_userRepository.User2.Id);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _serversService.DeleteChannelMessageAsync(
                _serversRepository.Server1.Id,
                _serversRepository.Channel1.Id,
                _serversRepository.Message1.Id));
    }
}