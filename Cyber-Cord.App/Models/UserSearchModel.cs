namespace Cyber_Cord.App.Models;

public class UserSearchModel
{
    private const int _defaultLimit = 10;

    public string? SearchName { get; set; }
    public int Limit { get; set; } = _defaultLimit;
    public bool SingleResultOnly { get; set; } = false;
}
