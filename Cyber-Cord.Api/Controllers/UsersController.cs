using Cyber_Cord.Api.Constants;
using Cyber_Cord.Api.Entities;
using Cyber_Cord.Api.Exceptions;
using Cyber_Cord.Api.Models;
using Cyber_Cord.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Shared;

namespace Cyber_Cord.Api.Controllers;

[Authorize(Roles = RoleNames.User)]
public class UsersController(IUsersService service) : BaseAuthorizationController
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> SearchUsers([FromQuery] UserSearchModel model)
    {
        var result = await service.SearchUsersAsync(model);

        return Ok(result);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await service.GetUserByIdAsync(id);

        return Ok(user);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var user = await service.GetCurrentUserAsync();

        return Ok(user);
    }

    [HttpGet("{id}/detail")]
    public async Task<IActionResult> GetUserDetail(int id)
    {
        var user = await service.GetUserDetailAsync(id);

        return Ok(user);
    }

    [HttpGet("{id}/settings")]
    public async Task<IActionResult> GetUserSettings(int id)
    {
        var settings = await service.GetUsersSettingsAsync(id);

        return Ok(settings);
    }

    [HttpGet("{id}/friends")]
    public async Task<IActionResult> SearchFriends(int id, [FromQuery] FriendSearchModel model)
    {
        var result = await service.SearchFriendsAsync(id, model);

        return Ok(result);
    }

    [HttpGet("{id}/pending")]
    public async Task<IActionResult> GetPendingRequests(int id)
    {
        var pending = await service.GetPendingRequestsAsync(id);

        return Ok(pending);
    }

    [HttpGet("{id}/friends/{friendshipId}/chat")]
    public async Task<IActionResult> GetFriendsChat(int id, int friendshipId)
    {
        var chat = await service.GetFriendsChatAsync(id, friendshipId);

        return Ok(chat);
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> CreateUser([FromBody] UserCreateModel model)
    {
        var user = await service.CreateUserAsync(model);

        return Ok(user);
    }

    [HttpPost("{id}/activate")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateUser(int id, [FromHeader (Name = Headers.ValidationTokenHeader)] string validationToken)
    {
        await service.ValidateUserAsync(id, validationToken);

        return NoContent();
    }

    [HttpPost("{id}/resendcode")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendValidationCode(int id)
    {
        var password = GetPassword();

        await service.ResendValidationCodeAsync(id, password);

        return Ok();
    }

    [HttpPost("{id}/friends")]
    public async Task<IActionResult> RequestFriendship(int id, [FromBody] FriendRequestCreateModel model)
    {
        await service.RequestFriendshipAsync(id, model);

        return Ok();
    }

    [HttpPost("{id}/pending/{friendshipId}/accept")]
    public async Task<IActionResult> AcceptFriendship(int id, int friendshipId)
    {
        var result = await service.AcceptFriendshipAsync(id, friendshipId);

        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateModel model)
    {
        var user = await service.UpdateUserAsync(id, model);

        return Ok(user);
    }

    // We use password here instead of a JWT so that somebody who has obtained a JWT would not be able to change the password
    [HttpPut("{id}/password")]
    public async Task<IActionResult> ChangeUserPassword(
        int id,
        [FromBody] UserPasswordChangeModel model
    ) {
       await service.ChangeUserPasswordAsync(id, model);

        return Ok();
    }

    [HttpPatch("{id}/settings")]
    public async Task<IActionResult> UpdateUserSettings(int id, [FromBody] JsonPatchDocument<Settings> document)
    {
        var settings = await service.UpdateUsersSettingsAsync(id, document);

        return Ok(settings);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var password = GetPassword();

        await service.DeleteUserAsync(id, password);

        return Ok();
    }

    [HttpDelete("{id}/friends/{friendshipId}")]
    public async Task<IActionResult> RemoveFriendship(int id, int friendshipId)
    {
        await service.RemoveFriendshipAsync(id, friendshipId);

        return NoContent();
    }

    [HttpPost("{id}/ping")]
    public async Task<IActionResult> Ping(int id)
    {
        await service.PingAsync(id);

        return Ok();
    }

    [HttpPost("{id}/roles")]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> AssignRolesToUser(int id, RolesAssignmentModel model)
    {
        await service.AssignRolesToUserAsync(id, model);

        return Ok();
    }

    private string GetPassword()
    {
        var claim = User.Claims.FirstOrDefault(x => x.Type == Headers.UserPasswordHeader);

        if (claim is null)
        {
            throw new ForbiddenException("This endpoint requires a password claim to be sent");
        }

        return claim.Value;
    }
}
