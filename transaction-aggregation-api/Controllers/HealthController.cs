using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace transaction_aggregation_api.Controllers
{
    // Answers the question "is the app alive and can it reach its database?"
    // Tools that watch servers (like a load balancer) call this every few seconds.
    //
    // It needs no login on purpose: those tools cannot log in, and the answer holds no customer data.
    // Example answer: 200 { "status": "Healthy", "checks": { "database": "Healthy" } }
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

        /// <summary>
        /// Is the app healthy? Gives 200 when everything works, and 503 when something is broken.
        /// </summary>
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
