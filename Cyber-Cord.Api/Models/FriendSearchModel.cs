using System.ComponentModel.DataAnnotations;
using Shared.Enums;

namespace Cyber_Cord.Api.Models;

public class FriendSearchModel
{
    // If not null, then we want only one result
    public int? SearchUserId { get; set; }
    public string? SearchName { get; set; }
    [Range(1, int.MaxValue)]
    public int Limit { get; set; } = int.MaxValue;
    [EnumDataType(enumType: typeof(Ordering))]
    public Ordering? Order { get; set; } = Ordering.Asc;
}
