using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using transaction_aggregation_api.Controllers;
using transaction_aggregation_api.Errors;
using transaction_aggregation_api.Json;
using transaction_aggregation_domain.Exceptions;

namespace transaction_aggregation_tests.Api
{
    public class ApiPartsTests
    {
        [Fact]
        public async Task ErrorHandler_NotFound_Becomes404_WithTheMessage()
        {
            var (status, body) = await HandleError(new NotFoundException("Account '9999999999' was not found."));

            Assert.Equal(404, status);
            Assert.Contains("Account '9999999999' was not found.", body);
        }

        [Fact]
        public async Task ErrorHandler_BadInput_Becomes400_WithTheMessage()
        {
            var (status, body) = await HandleError(new ArgumentException("The account number must be exactly 10 digits."));

            Assert.Equal(400, status);
            Assert.Contains("The account number must be exactly 10 digits.", body);
        }

        [Fact]
        public async Task ErrorHandler_AnyOtherError_Becomes500_AndHidesTheDetails()
        {
            var (status, body) = await HandleError(new InvalidOperationException("Data Source=secret-server"));

            Assert.Equal(500, status);
            Assert.DoesNotContain("secret-server", body);
            Assert.Contains("Please try again later.", body);
        }

        [Theory]
        [InlineData("14500", "14500.00")]
        [InlineData("-5989.7", "-5989.70")]
        [InlineData("0", "0.00")]
        [InlineData("79.90", "79.90")]
        public void RandConverter_AlwaysWritesTwoDecimals(string amount, string expected)
        {
            var json = JsonSerializer.Serialize(decimal.Parse(amount, CultureInfo.InvariantCulture), RandOptions());

            Assert.Equal(expected, json);
        }

        [Fact]
        public void RandConverter_ReadsAmountsBackIn()
        {
            Assert.Equal(12.5m, JsonSerializer.Deserialize<decimal>("12.5", RandOptions()));
        }

        [Fact]
        public async Task Health_AllGood_Returns200()
        {
            var result = Assert.IsAssignableFrom<ObjectResult>(await new HealthController(new FakeHealthCheckService(HealthStatus.Healthy)).GetHealth());

            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task Health_DatabaseDown_Returns503()
        {
            var result = Assert.IsAssignableFrom<ObjectResult>(await new HealthController(new FakeHealthCheckService(HealthStatus.Unhealthy)).GetHealth());

            Assert.Equal(503, result.StatusCode);
        }

        [Fact]
        public void Startup_FapiOnButNoAuthority_StopsWithAClearMessage()
        {
            using var factory = new FapiApiFactory().WithWebHostBuilder(builder => builder.UseSetting("Fapi:Authority", ""));

            var error = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

            Assert.Contains("Fapi:Authority", error.ToString());
        }

        private static async Task<(int Status, string Body)> HandleError(Exception exception)
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            var handled = await new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance).TryHandleAsync(context, exception, CancellationToken.None);

            Assert.True(handled);
            Assert.Equal("application/problem+json", context.Response.ContentType);

            context.Response.Body.Position = 0;
            return (context.Response.StatusCode, await new StreamReader(context.Response.Body).ReadToEndAsync());
        }

        private static JsonSerializerOptions RandOptions()
        {
            return new JsonSerializerOptions { Converters = { new RandJsonConverter() } };
        }

        private class FakeHealthCheckService : HealthCheckService
        {
            private readonly HealthStatus _status;

            public FakeHealthCheckService(HealthStatus status)
            {
                _status = status;
            }

            public override Task<HealthReport> CheckHealthAsync(Func<HealthCheckRegistration, bool>? predicate, CancellationToken cancellationToken = default)
            {
                var entries = new Dictionary<string, HealthReportEntry>
                {
                    ["database"] = new HealthReportEntry(_status, null, TimeSpan.Zero, null, null)
                };

                return Task.FromResult(new HealthReport(entries, TimeSpan.Zero));
            }
        }
    }
}
