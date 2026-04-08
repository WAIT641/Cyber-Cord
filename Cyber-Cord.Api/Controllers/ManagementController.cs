using Cyber_Cord.Api.Constants;
using Cyber_Cord.Api.Models;
using Cyber_Cord.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cyber_Cord.Api.Controllers;

[Authorize]
public class ManagementController(IManagementService service) : BaseAuthorizationController
{
    [HttpGet("latency")]
    [AllowAnonymous]
    public IActionResult Latency([FromQuery(Name = Shared.Headers.LatencyHeader)] string userTimeSent)
    {
        var latency = LatencyService.GetLatency(userTimeSent);
        
        return Ok(latency);
    }

    [HttpGet("logs")]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> GetLogs([FromQuery] LogFilterModel filter)
    {
        var logs = await service.GetLogsAsync(filter);

        return Ok(logs);
    }
}