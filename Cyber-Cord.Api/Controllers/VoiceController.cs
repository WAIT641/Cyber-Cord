using Cyber_Cord.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cyber_Cord.Api.Controllers;

public class VoiceController(IVoiceService voiceService) : BaseAuthorizationController
{
    [HttpPost("rooms/{roomId}/token")]
    public async Task<IActionResult> GetToken(string roomId)
    {
        var token = await voiceService.GenerateTokenAsync(roomId);

        return Ok(token);
    }
    
    [HttpGet("rooms/{roomId}")]
    public async Task<IActionResult> GetRoomInfo(string roomId)
    {
        var info = await voiceService.GetRoomInfoAsync(roomId);
        return Ok(info);
    }
    
    [HttpGet("rooms/{roomId}/participants")]
    public async Task<IActionResult> GetParticipants(string roomId)
    {
        var participants = await voiceService.GetParticipantsAsync(roomId);
        return Ok(participants);
    }
    
    [HttpDelete("rooms/{roomId}/participants/{userId}")]
    public async Task<IActionResult> RemoveParticipant(string roomId, string userId)
    {
        await voiceService.RemoveParticipantAsync(roomId, userId);
        return NoContent();
    }
    
    [HttpDelete("rooms/{roomId}")]
    public async Task<IActionResult> DeleteRoom(string roomId)
    {
        await voiceService.DeleteRoomAsync(roomId);
        return NoContent();
    }
}