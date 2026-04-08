namespace Cyber_Cord.Api.Models;

public class LatencyResponse
{
    public long ClientTimestamp { get; set; }
    public long ServerReceivedTimestamp { get; set; }
    public long ServerSentTimestamp { get; set; }
}