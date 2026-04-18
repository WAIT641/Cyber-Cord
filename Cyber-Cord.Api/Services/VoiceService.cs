using Livekit.Server.Sdk.Dotnet;
using Microsoft.Extensions.Options;
using ListRoomsRequest = Livekit.Server.Sdk.Dotnet.ListRoomsRequest;

namespace Cyber_Cord.Api.Services;

public class VoiceService(IOptions<LiveKitOptions> options, IUsersService usersService) : IVoiceService {
    private LiveKitOptions _options => options.Value;
    private readonly RoomServiceClient _roomClient = new(
        options.Value.ServerUrl,
        options.Value.ApiKey,
        options.Value.ApiSecret
        );

    public async Task<VoiceTokenDto> GenerateTokenAsync(string roomId)
    {
        var user = await usersService.GetCurrentUserAsync();
        
        var token = new AccessToken(_options.ApiKey, _options.ApiSecret)
            .WithIdentity(user.Id.ToString())
            .WithName(user.DisplayName)
            .WithTtl(TimeSpan.FromHours(_options.TokenTtlHours))
            .WithGrants(new VideoGrants
            {
                RoomJoin  = true,
                Room      = roomId,
                CanPublish = true,       // send audio
                CanSubscribe = true,     // receive audio from others
                CanPublishData = true,   // allows mute state sync etc.
                RoomList = true,
            });

        var jwtToken = await Task.FromResult(token.ToJwt());

        var voiceToken = new VoiceTokenDto(
            Token: jwtToken,
            RoomId: roomId,
            ServerUrl: "ws://localhost:7880"
        );
        
        return voiceToken;
    }

    public async Task<RoomInfoDto> GetRoomInfoAsync(string roomId)
    {
        var request = new ListRoomsRequest();
        var response = await _roomClient.ListRooms(request);

        var room = response.Rooms.FirstOrDefault(r => r.Name == roomId);
        
        if (room is null)
            return new RoomInfoDto(roomId, roomId, 0, false);
        
        return new RoomInfoDto(
            RoomId:           room.Name,
            RoomName:         room.Name,
            ParticipantCount: (int)room.NumParticipants,
            IsActive:         room.NumParticipants > 0
        );
    }

    public async Task<IEnumerable<ParticipantDto>> GetParticipantsAsync(string roomId)
    {
        var request = new ListParticipantsRequest { Room = roomId };
        var response = await _roomClient.ListParticipants(request);

        return response.Participants.Select(p => new ParticipantDto(
            UserId:      p.Identity,
            DisplayName: p.Name,
            IsSpeaking:  false,   // speaking state is real-time only, not available via API
            IsMuted:     !p.Tracks.Any(t => t.Source == TrackSource.Microphone && !t.Muted),
            JoinedAt:    DateTimeOffset.FromUnixTimeSeconds(p.JoinedAt).UtcDateTime
        ));
    }

    public async Task RemoveParticipantAsync(string roomId, string userId)
    {
        var request = new RoomParticipantIdentity
        {
            Room     = roomId,
            Identity = userId,
        };

        await _roomClient.RemoveParticipant(request);
    }

    public async Task DeleteRoomAsync(string roomId)
    {
        var request = new DeleteRoomRequest { Room = roomId };
        await _roomClient.DeleteRoom(request);
    }
}