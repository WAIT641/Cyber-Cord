namespace Cyber_Cord.Api.Services;

public interface IVoiceService
{
    Task<VoiceTokenDto> GenerateTokenAsync(string roomId);
    Task<RoomInfoDto> GetRoomInfoAsync(string roomId);
    Task<IEnumerable<ParticipantDto>> GetParticipantsAsync(string roomId);
    Task<VoiceTokenDto> GenerateTokenAsync<T>(int baseId);
    Task RemoveParticipantAsync(string roomId, string userId);
    Task DeleteRoomAsync(string roomId);
}