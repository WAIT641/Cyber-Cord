using Cyber_Cord.Api.Services;
using Livekit.Server.Sdk.Dotnet;
using Microsoft.Extensions.Options;
using ListRoomsRequest = Livekit.Server.Sdk.Dotnet.ListRoomsRequest;

public class VoiceService : IVoiceService
{
    private readonly LiveKitSettings _settings;
    private readonly RoomServiceClient _roomClient;
    private ICurrentUserContext _userContext;
    private IUsersService _usersService;

    private int UserId => _userContext.GetId();
    
    public VoiceService(IOptions<LiveKitSettings> settings, ICurrentUserContext context, IUsersService service)
    {
        _settings = settings.Value;
        _roomClient = new RoomServiceClient(_settings.ServerUrl, _settings.ApiKey, _settings.ApiSecret);
        _userContext = context;
        _usersService = service;
    }

    // -------------------------------------------------------------------------
    // Token generation
    // -------------------------------------------------------------------------

    public async Task<VoiceTokenDto> GenerateTokenAsync(string roomId)
    {
        var user = await _usersService.GetCurrentUserAsync();
        
        var token = new AccessToken(_settings.ApiKey, _settings.ApiSecret)
            .WithIdentity(user.Id.ToString())
            .WithName(user.DisplayName)
            .WithTtl(TimeSpan.FromHours(_settings.TokenTtlHours))
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

    // -------------------------------------------------------------------------
    // Room info
    // -------------------------------------------------------------------------

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

    // -------------------------------------------------------------------------
    // Participants
    // -------------------------------------------------------------------------

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

    // -------------------------------------------------------------------------
    // Moderation
    // -------------------------------------------------------------------------

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
