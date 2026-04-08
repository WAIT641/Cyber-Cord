using Cyber_Cord.Api.Constants;
using Cyber_Cord.Api.Models;
using Cyber_Cord.Api.Models.Base;
using Cyber_Cord.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Models.ApiModels;

namespace Cyber_Cord.Api.Controllers;

[Authorize(Roles = RoleNames.User)]
public class ChatsController(IChatsService service) : BaseAuthorizationController
{
    [HttpGet]
    public async Task<IActionResult> SearchChats([FromQuery] ChatSearchModel model)
    {
        var result = await service.SearchChatsAsync(model);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetChatById(int id)
    {
        var chat = await service.GetChatByIdAsync(id);

        return Ok(chat);
    }

    [HttpGet("{id}/users")]
    public async Task<IActionResult> GetChatUsers(int id)
    {
        var result = await service.GetChatUsersAsync(id);

        return Ok(result);
    }

    [HttpGet("{id}/messages")]
    public async Task<IActionResult> GetChatMessages(int id, [FromQuery] CursorPaginationFilter filter)
    {
        var result = await service.GetChatMessagesAsync(id, filter);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateChat([FromBody] ChatCreateModel model)
    {
        var chat = await service.CreateChatAsync(model);

        return Ok(chat);
    }

    [HttpPost("{id}/users")]
    public async Task<IActionResult> AddUserToChat(int id, [FromBody] UserChatCreateModel model) 
    { await service.AddUserToChatAsync(id, model);

        return NoContent();
    }

    [HttpPost("{id}/messages")]
    public async Task<IActionResult> PostMessageToChat(int id, [FromBody] MessageCreateModel model)
    {
        var result = await service.PostMessageToChatAsync(id, model);

        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateChat(int id, [FromBody] ChatUpdateModel model)
    {
        var chat = await service.UpdateChatAsync(id, model);

        return Ok(chat);
    }

    [HttpPut("{id}/messages/{messageId}")]
    public async Task<IActionResult> UpdateMessageAsync(
        int id,
        int messageId,
        [FromBody] MessageUpdateModel model
    ) {
        var message = await service.UpdateMessageAsync(id, messageId, model);

        return Ok(message);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteChat(int id)
    {
        await service.DeleteChatAsync(id);

        return NoContent();
    }

    [HttpDelete("{id}/users/{userId}")]
    public async Task<IActionResult> RemoveUserFromChat(int id, int userId)
    {
        await service.RemoveUserFromChatAsync(id, userId);

        return NoContent();
    }

    [HttpDelete("{id}/messages/{messageId}")]
    public async Task<IActionResult> DeleteMessageFromChat(int id, int messageId)
    {
        await service.DeleteMessageFromChatAsync(id, messageId);

        return NoContent();
    }

    [HttpPost("{id}/call")]
    public async Task<IActionResult> HandleCall(int id, [FromBody] CallMessageModel messageModel)
    {
        await service.HandleCall(id, messageModel);
        
        return Ok();
    }
}