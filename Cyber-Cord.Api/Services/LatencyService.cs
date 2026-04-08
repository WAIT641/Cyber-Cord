using Cyber_Cord.Api.Exceptions;
using Cyber_Cord.Api.Models;

namespace Cyber_Cord.Api.Services;

public static class LatencyService
{
    public static LatencyResponse GetLatency(string userTimeSent)
    {
        var serverReceived = DateTime.UtcNow.Ticks;

        if (!long.TryParse(userTimeSent, out long time))
            throw new BadRequestException("Wrong time format");
        
        return new LatencyResponse
        {
            ClientTimestamp = time,
            ServerReceivedTimestamp = serverReceived,
            ServerSentTimestamp = DateTime.UtcNow.Ticks,
        };
    }
}