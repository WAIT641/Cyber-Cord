using System.ComponentModel.DataAnnotations;
using Cyber_Cord.Api.Models.Base;

namespace Cyber_Cord.Api.Models;

public class LogFilterModel : PaginationFilter
{
    public List<string>? LogLevels { get; set; } = default!;
    public DateTime? After { get; set; } = default!;
    public DateTime? Before { get; set; } = default!;
}
