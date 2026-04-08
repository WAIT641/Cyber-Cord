namespace Cyber_Cord.Api.Entities;

public class LogEntry
{
    public int Id { get; set; }
    public string Message { get; set; } = default!;
    public string MessageTemplate { get; set; } = default!;
    public string Level { get; set; } = default!;
    public DateTime Timestamp { get; set; }
    public string Exception { get; set; } = default!;
    public string Properties { get; set; } = default!;
    public string LogEvent { get; set; } = default!;
}
