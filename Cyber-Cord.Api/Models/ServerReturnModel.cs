namespace Cyber_Cord.Api.Models;

public class ServerReturnModel
{
    public int Id { get; set; }
    
    public required string Name { get; set; }
    
    public required string Description { get; set; }
    
    public int OwnerId { get; set; }
}