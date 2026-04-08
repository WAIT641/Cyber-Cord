using Cyber_Cord.Api.Data;
using Cyber_Cord.Api.Entities;
using Cyber_Cord.Api.Extensions;
using Cyber_Cord.Api.Models;
using Cyber_Cord.Api.Models.Base;

namespace Cyber_Cord.Api.Services;

public class ManagementService(AppDbContext context) : IManagementService
{
    public async Task<PaginatedResult<LogEntry>> GetLogsAsync(LogFilterModel filter)
    {
        var query = context.LogEvents.AsQueryable();

        if (filter.LogLevels is not null && filter.LogLevels.Any())
        {
            query = query.Where(l => filter.LogLevels.Contains(l.Level));
        }
        if (filter.After is not null)
        {
            query = query.Where(l => l.Timestamp >= filter.After.Value);
        }
        if (filter.Before is not null)
        {
            query = query.Where(l => l.Timestamp <= filter.Before.Value);
        }

        var result = await query.ToPaginatedAsync(filter);

        return result;
    }
}
