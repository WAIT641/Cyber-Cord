using Cyber_Cord.Api.Entities;
using Cyber_Cord.Api.Models;
using Cyber_Cord.Api.Models.Base;

namespace Cyber_Cord.Api.Services;
public interface IManagementService
{
    Task<PaginatedResult<LogEntry>> GetLogsAsync(LogFilterModel filter);
}