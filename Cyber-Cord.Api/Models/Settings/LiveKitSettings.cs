public class LiveKitSettings
{
    public const string Section = "LiveKit";

    public string ServerUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public int TokenTtlHours { get; set; } = 1;
}
