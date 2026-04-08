using System.ComponentModel.DataAnnotations;
using Shared.Enums;

namespace Cyber_Cord.Api.Models;

public class ChatSearchModel
{
    private const int _defaultLimit = 10;
    private const int _maxLimit = 50;

    public string? SearchName { get; set; }
    [EnumDataType(enumType: typeof(Ordering))]
    public Ordering? Order { get; set; } = Ordering.Asc;
    [Range(1, _maxLimit)]
    public int Limit { get; set; } = _defaultLimit;
}
