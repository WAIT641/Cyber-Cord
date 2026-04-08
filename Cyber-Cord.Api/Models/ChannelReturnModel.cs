namespace Cyber_Cord.Api.Models;

public class ChannelReturnModel
{
    public int Id { get; set; }
    public int ServerId { get; set; }
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
}