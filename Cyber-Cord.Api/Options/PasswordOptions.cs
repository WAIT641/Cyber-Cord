namespace Cyber_Cord.Api.Options;

public class PasswordOptions
{
    public string? Pepper { get; set; }
    public int? SaltLength { get; set; }
    public int? HashLength { get; set; }
    public int? Iterations { get; set; }
    public int? Lanes { get; set; }
    public int? Threads { get; set; }
    public int? MemoryCost { get; set; }
}
