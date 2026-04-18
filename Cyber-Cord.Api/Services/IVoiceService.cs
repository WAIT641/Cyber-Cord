using Google.Protobuf.Collections;
using LiveKit.Proto;

public interface IVoiceService
{
    Task<VoiceTokenDto> GenerateTokenAsync(string roomId);
    Task<RoomInfoDto> GetRoomInfoAsync(string roomId);
    Task<IEnumerable<ParticipantDto>> GetParticipantsAsync(string roomId);
    Task RemoveParticipantAsync(string roomId, string userId);
    Task DeleteRoomAsync(string roomId);
}
