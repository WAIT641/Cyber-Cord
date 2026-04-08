namespace Cyber_Cord.App.Models;

public class LatencyRecieveModel
{
    public long ClientTimestamp { get; set; }
    public long ServerReceivedTimestamp { get; set; }
    public long ServerSentTimestamp { get; set; }
}