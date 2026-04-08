using Cyber_Cord.Api.Constants;
using Cyber_Cord.Api.Models;
using Cyber_Cord.Api.Models.Base;
using Cyber_Cord.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cyber_Cord.Api.Controllers;

[Authorize(Roles = RoleNames.User)]
public class ServersController(IServersService serversService) : BaseAuthorizationController
{
    [HttpGet]
    public async Task<IActionResult> GetUserServers()
    {
        var result = await serversService.GetAllServersAsync();

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetServerById(int id)
    {   
        var server = await serversService.GetServerByIdAsync(id);

        var returnModel = new ServerReturnModel
        {
            Id = server.Id,
            Name = server.Name,
            Description = server.Description,
            OwnerId = server.OwnerId
        };

        return Ok(returnModel);
    }

    [HttpPost]
    public async Task<IActionResult> CreateServer([FromBody] ServerCreateModel model)
    {   
        var server = await serversService.CreateServerAsync(model.Name!, model.Description ?? string.Empty);

        return Ok(server);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateServer(int id, [FromBody] ServerUpdateModel model)
    {   
        var server = await serversService.UpdateServerAsync(id, model.Name, model.Description);

        return Ok(server);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteServer(int id)
    {        
        await serversService.DeleteServerAsync(id);

        return NoContent();
    }

    [HttpGet("{id}/members")]
    public async Task<IActionResult> GetServerMembers(int id, [FromQuery] int limit = 50)
    {
        var members = await serversService.GetServerMembersAsync(id, limit);

        return Ok(members);
    }

    [HttpPost("{id}/members")]
    public async Task<IActionResult> AddUserToServer(
        int id,
        [FromBody] UserServerCreateModel model
    ) {   
        await serversService.AddUserToServerAsync(id, model.MemberId!.Value);

        return NoContent();
    }

    [HttpDelete("{id}/members/{memberId}")]
    public async Task<IActionResult> RemoveUserFromServer(int id, int memberId)
    {  

        await serversService.RemoveUserFromServerAsync(id, memberId);
        
        return NoContent();
    }
    
    [HttpPost("{id}/owner/{newOwnerId}")]
    public async Task<IActionResult> TransferServerOwnershipAsync(int id, int newOwnerId)
    {
        await serversService.TransferServerOwnershipAsync(id, newOwnerId);
        return NoContent();
    }
    

    [HttpGet("{id}/bans")]
    public async Task<IActionResult> GetServerBannedUsers(int id)
    {
        var result = await serversService.GetServerBannedUsersAsync(id);

        return Ok(result);
    }

    [HttpPost("{id}/bans/{bannedUserId}")]
    public async Task<IActionResult> BanUserFromServer(int id, int bannedUserId)
    {   
        await serversService.BanUserFromServerAsync(id, bannedUserId);
        
        return NoContent();
    }

    [HttpDelete("{id}/bans/{bannedUserId}")]
    public async Task<IActionResult> UnbanUserFromServer(int id, int bannedUserId)
    {   

       await serversService.UnbanUserFromServerAsync(id, bannedUserId);

        return NoContent();
    }
    
    [HttpGet("{serverId}/channels")]
    public async Task<IActionResult> GetServerChannels(int serverId)
    {
        var channels = await serversService.GetServerChannelsAsync(serverId);

        return Ok(channels);
    }

    [HttpGet("{serverId}/channels/{channelId}")]
    public async Task<IActionResult> GetChannelById(int serverId, int channelId)
    {
        var channel = await serversService.GetChannelByIdAsync(channelId, serverId);

        return Ok(channel);
    }

    [HttpGet("{serverId}/channels/{channelId}/messages")]
    public async Task<IActionResult> GetChannelMessages(
        int serverId, 
        int channelId, 
        [FromQuery] CursorPaginationFilter filter
    ) {
        var messages = await serversService.GetChannelMessagesAsync(channelId, serverId, filter);

        return Ok(messages);
    }
    
    [HttpPut("{serverId}/channels/{channelId}/messages/{messageId}")]
    public async Task<IActionResult> UpdateChannelMessage(
        int serverId,
        int channelId,
        int messageId,
        [FromBody] MessageUpdateModel model
    ) {
        var message = await serversService.UpdateChannelMessageAsync(serverId, channelId, messageId, model);

        return Ok(message);
    }
    
    [HttpDelete("{serverId}/channels/{channelId}/messages/{messageId}")]
    public async Task<IActionResult> DeleteChannelMessage(
        int serverId,
        int channelId,
        int messageId
    ) {
        await serversService.DeleteChannelMessageAsync(serverId, channelId, messageId);
        
        return NoContent();
    }

    [HttpPost("{serverId}/channels")]
    public async Task<IActionResult> CreateChannel(
        int serverId,
        [FromBody] ChannelCreateModel model
    ) {
        var channel = await serversService.CreateChannelAsync(serverId, model);

        return Ok(channel);
    }

    [HttpPut("{serverId}/channels/{channelId}")]
    public async Task<IActionResult> UpdateChannel(
        int serverId,
        int channelId,
        [FromBody] ChannelUpdateModel model
    ) {
        var channel = await serversService.UpdateChannelAsync(channelId, serverId, model);

        return Ok(channel);
    }

    [HttpDelete("{serverId}/channels/{channelId}")]
    public async Task<IActionResult> DeleteChannel(
        int serverId,
        int channelId
    ) {
        await serversService.DeleteChannelAsync(channelId, serverId);
        
        return NoContent();
    }

    [HttpPost("{serverId}/channels/{channelId}/messages")]
    public async Task<IActionResult> SendMessage(
        int serverId,
        int channelId,
        [FromBody] MessageCreateModel model
    ) {
        var message = await serversService.SendMessageAsync(channelId, serverId, model);

        return Ok(message);
    }
}