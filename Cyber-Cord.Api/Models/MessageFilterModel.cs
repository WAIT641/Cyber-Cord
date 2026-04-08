using System.ComponentModel.DataAnnotations;

namespace Cyber_Cord.Api.Models;

public class MessageFilterModel
{
    private const int _defaultLimit = 10;
    private const int _maxLimit = 50;

    public int Begin { get; set; } = 0;
    [Range(0, _maxLimit)]
    public int Count { get; set; } = _defaultLimit;
}
