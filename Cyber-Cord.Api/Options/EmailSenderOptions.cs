namespace Cyber_Cord.Api.Options;

public class EmailSenderOptions
{
    public string? EmailAuthKey { get; set; }
    public string EmailHost { get; set; } = default!;
    public string SourceEmailAddress { get; set; } = default!;
    public string EmailUsername { get; set; } = default!;
}
