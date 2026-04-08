using System.ComponentModel.DataAnnotations;
using Shared.Enums;

namespace Cyber_Cord.Api.Models;

public class UserSearchModel
{
    private const int _defaultLimit = 10;
    private const int _maxLimit = 50;

    public string? SearchName { get; set; }
    [Range(1, _maxLimit)]
    public int Limit { get; set; } = _defaultLimit;
    [EnumDataType(enumType: typeof(Ordering))]
    public Ordering? Order { get; set; } = Ordering.Asc;
    public bool ActivatedOnly { get; set; } = true;
    public bool SingleResultOnly { get; set; }
}
