namespace Cyber_Cord.App.Models;

public class ChannelModel
{
    public int Id { get; set; }
    public int ServerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}