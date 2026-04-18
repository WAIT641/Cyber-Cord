using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Extensions.Options;

[ApiController]
[Route("api/[controller]")]
public class VoiceController : ControllerBase
{
    private readonly IVoiceService _voiceService;
    private readonly LiveKitSettings _settings;

    public VoiceController(IVoiceService voiceService, IOptions<LiveKitSettings> settings)
    {
        _voiceService = voiceService;
        _settings     = settings.Value;
    }

    // POST api/voice/rooms/{roomId}/token
    // Called by the Blazor client before joining a voice channel
    [HttpPost("rooms/{roomId}/token")]
    public async Task<IActionResult> GetToken(string roomId)
    {
        var token = await _voiceService.GenerateTokenAsync(roomId);

        return Ok(token);
    }

    // GET api/voice/rooms/{roomId}
    // Get room status (participant count, active etc.)
    [HttpGet("rooms/{roomId}")]
    public async Task<IActionResult> GetRoomInfo(string roomId)
    {
        var info = await _voiceService.GetRoomInfoAsync(roomId);
        return Ok(info);
    }

    // GET api/voice/rooms/{roomId}/participants
    // List who is currently in the voice channel
    [HttpGet("rooms/{roomId}/participants")]
    public async Task<IActionResult> GetParticipants(string roomId)
    {
        var participants = await _voiceService.GetParticipantsAsync(roomId);
        return Ok(participants);
    }

    // DELETE api/voice/rooms/{roomId}/participants/{userId}
    // Kick a user from a voice channel (admin/moderator only)
    [HttpDelete("rooms/{roomId}/participants/{userId}")]
    public async Task<IActionResult> RemoveParticipant(string roomId, string userId)
    {
        await _voiceService.RemoveParticipantAsync(roomId, userId);
        return NoContent();
    }

    // DELETE api/voice/rooms/{roomId}
    // Force-close a voice channel
    [HttpDelete("rooms/{roomId}")]
    public async Task<IActionResult> DeleteRoom(string roomId)
    {
        await _voiceService.DeleteRoomAsync(roomId);
        return NoContent();
    }
}
