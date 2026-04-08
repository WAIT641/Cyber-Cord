using System.Text.Json.Serialization;

namespace Cyber_Cord.App.Models;

public class PaginatedMessages
{
    [JsonRequired]
    public List<MessageModel> Data { get; set; } = [];
    [JsonRequired]
    public int TotalCount { get; set; }
    [JsonRequired]
    public int PageSize { get; set; }
    [JsonRequired]
    public CursorModel Cursor { get; set; } = default!;
}
