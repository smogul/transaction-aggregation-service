using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace transaction_aggregation_api.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        private readonly HealthCheckService _healthCheckService;

        public HealthController(HealthCheckService healthCheckService)
        {
            _healthCheckService = healthCheckService;
        }

        [HttpGet]
        public async Task<IActionResult> GetHealth()
        {
            var report = await _healthCheckService.CheckHealthAsync();

            var answer = new
            {
                Status = report.Status.ToString(),
                Checks = report.Entries.ToDictionary(check => check.Key, check => check.Value.Status.ToString())
            };

            return report.Status == HealthStatus.Healthy
                ? Ok(answer)
                : StatusCode(StatusCodes.Status503ServiceUnavailable, answer);
        }
    }
}
