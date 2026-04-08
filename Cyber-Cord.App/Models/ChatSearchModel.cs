namespace Cyber_Cord.App.Models;

public class ChatSearchModel
{
    private const int _defaultLimit = 50;

    public string? SearchName { get; set; }
    public int Limit { get; set; } = _defaultLimit;
}