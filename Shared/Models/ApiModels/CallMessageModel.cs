namespace Shared.Models.ApiModels;

public class CallMessageModel
{
    public enum MessageType
    {
        Start,
        Reject,
        End,
        Accept
    }

    public MessageType Type { get; set; }
    public int OriginatingUserId { get; set; }
    public string OriginatingUserName { get; set; }
    public string Sdp { get; set; }
}