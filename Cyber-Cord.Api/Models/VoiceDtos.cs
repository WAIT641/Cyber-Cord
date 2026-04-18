public record RoomInfoDto(
    string RoomId,
    string RoomName,
    int ParticipantCount,
    bool IsActive
);

public record ParticipantDto(
    string UserId,
    string DisplayName,
    bool IsSpeaking,
    bool IsMuted,
    DateTime JoinedAt
);

public record VoiceTokenDto(
    string Token,
    string RoomId,
    string ServerUrl
);
