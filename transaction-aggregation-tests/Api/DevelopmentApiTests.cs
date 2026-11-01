using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using transaction_aggregation_tests.Fakes;

namespace transaction_aggregation_tests.Api
{
    public class DevelopmentApiTests : IClassFixture<DevelopmentApiFactory>
    {
        private readonly HttpClient _client;

        public DevelopmentApiTests(DevelopmentApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Health_IsHealthy()
        {
            var response = await _client.GetAsync("/health");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("\"database\":\"Healthy\"", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task GetAccount_NeedsNoToken_AndShowsMoneyWithTwoDecimals()
        {
            var json = await _client.GetStringAsync("/api/accounts/1000000001");

            Assert.Contains("\"id\":\"1000000001\"", json);
            Assert.Contains("\"openingBalance\":5000.00", json);
        }

        [Fact]
        public async Task GetBalance_AddsUpAllFiveDataSources()
        {
            var json = await _client.GetStringAsync("/api/accounts/1000000002/balance");

            Assert.Contains("\"totalCredit\":8354.95", json);
            Assert.Contains("\"totalDebit\":2500.00", json);
            Assert.Contains("\"currentBalance\":25854.95", json);
        }

        [Theory]
        [InlineData("/api/accounts/12345", HttpStatusCode.BadRequest)]
        [InlineData("/api/accounts/ACC-1001", HttpStatusCode.BadRequest)]
        [InlineData("/api/accounts/9999999999", HttpStatusCode.NotFound)]
        [InlineData("/api/accounts/12345/balance", HttpStatusCode.BadRequest)]
        [InlineData("/api/accounts/9999999999/balance", HttpStatusCode.NotFound)]
        [InlineData("/api/accounts/12345/statements", HttpStatusCode.BadRequest)]
        [InlineData("/api/statements/hello/download", HttpStatusCode.BadRequest)]
        [InlineData("/api/statements/3f2b6c1e-8d4a-4c3b-9e51-2a7d0f6b1c9e/download", HttpStatusCode.NotFound)]
        public async Task BadInput_GetsAProblemDetailsAnswer(string url, HttpStatusCode expected)
        {
            var response = await _client.GetAsync(url);

            Assert.Equal(expected, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task AccountNumberWithSpaces_IsTrimmed()
        {
            var json = await _client.GetStringAsync("/api/accounts/%201000000001%20");

            Assert.Contains("\"id\":\"1000000001\"", json);
        }

        [Theory]
        [InlineData("/api/accounts/1000000001/transactions")]
        [InlineData("/api/accounts/1000000001/transactions/summary")]
        [InlineData("/api/customers/CUST-001/balances")]
        [InlineData("/api/transactions/categories")]
        [InlineData("/mock-bank/accounts/2000000001/transactions")]
        [InlineData("/api/statements/download/ABC123")]
        [InlineData("/connect/token")]
        public async Task RemovedEndpoints_AreGone(string url)
        {
            Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync(url)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await _client.PostAsync(url, null)).StatusCode);
        }

        [Fact]
        public async Task CreateStatement_Returns201_PointingAtTheStatementList()
        {
            var response = await _client.PostAsJsonAsync("/api/accounts/1000000001/statements", new { fromDate = "2026-09-01", toDate = "2026-09-30" });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("/api/accounts/1000000001/statements", response.Headers.Location?.AbsolutePath);
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("{\"fromDate\":\"2026-09-01\"}")]
        [InlineData("{\"fromDate\":\"yesterday\",\"toDate\":\"2026-09-30\"}")]
        [InlineData("{\"fromDate\":\"2026-10-01\",\"toDate\":\"2026-09-30\"}")]
        public async Task CreateStatement_BadDates_Returns400(string body)
        {
            var response = await _client.PostAsync("/api/accounts/1000000001/statements", new StringContent(body, Encoding.UTF8, "application/json"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Statement_CreateListLinkAndDownload()
        {
            var created = await ReadJson(await _client.PostAsJsonAsync("/api/accounts/1000000001/statements", new { fromDate = "2026-09-01", toDate = "2026-09-30" }));
            var statementId = created.GetProperty("id").GetString();

            var list = await _client.GetFromJsonAsync<JsonElement>("/api/accounts/1000000001/statements");
            var listed = list.EnumerateArray().Single(statement => statement.GetProperty("id").GetString() == statementId);
            Assert.EndsWith("Z", listed.GetProperty("createdAt").GetString());

            Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/statements/{statementId}/download")).StatusCode);

            var link = await ReadJson(await _client.PostAsync($"/api/statements/{statementId}/download-link", null));
            Assert.EndsWith($"/api/statements/{statementId}/download", link.GetProperty("url").GetString());

            var download = await _client.GetAsync($"/api/statements/{statementId}/download");
            Assert.Equal(HttpStatusCode.OK, download.StatusCode);
            Assert.Equal("application/pdf", download.Content.Headers.ContentType?.MediaType);
            Assert.True(download.Headers.CacheControl?.NoStore);
            Assert.Equal("statement-1000000001-2026-09-01-to-2026-09-30.pdf", download.Content.Headers.ContentDisposition?.FileNameStar);
            Assert.True(IsPdf(await download.Content.ReadAsByteArrayAsync()));
        }

        [Fact]
        public async Task UploadWordFile_MakesAStatementWithNoPeriod_ThatCanBeDownloaded()
        {
            var response = await Upload("1000000001", "september.docx", TestFiles.CreateWordFile("Absa statement", "01 Sep SALARY 25000.00"));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var statement = await ReadJson(response);
            Assert.Equal(JsonValueKind.Null, statement.GetProperty("fromDate").ValueKind);

            var statementId = statement.GetProperty("id").GetString();
            await _client.PostAsync($"/api/statements/{statementId}/download-link", null);
            var download = await _client.GetAsync($"/api/statements/{statementId}/download");

            Assert.Equal(HttpStatusCode.OK, download.StatusCode);
            Assert.Equal($"statement-1000000001-{DateTime.UtcNow:yyyy-MM-dd}.pdf", download.Content.Headers.ContentDisposition?.FileNameStar);
            Assert.True(IsPdf(await download.Content.ReadAsByteArrayAsync()));
        }

        [Theory]
        [InlineData("tool.exe", "MZ")]
        [InlineData("fake.pdf", "hello")]
        [InlineData("empty.txt", "")]
        public async Task Upload_BadFile_Returns400(string fileName, string content)
        {
            var response = await Upload("1000000001", fileName, Encoding.ASCII.GetBytes(content));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Upload_WithoutAFile_Returns400()
        {
            var response = await _client.PostAsync("/api/accounts/1000000001/statements/upload", new MultipartFormDataContent { { new StringContent("x"), "Other" } });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("The File field is required.", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Upload_UnknownAccount_Returns404()
        {
            var response = await Upload("9999999999", "note.txt", Encoding.ASCII.GetBytes("hello"));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task StatementDownloadLink_ForAPeriod_DownloadsAStatementNamedAfterThatPeriod()
        {
            var response = await _client.PostAsJsonAsync("/api/accounts/1000000001/statements/download-link", new { fromDate = "2026-09-01", toDate = "2026-09-30" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var link = await ReadJson(response);
            Assert.StartsWith("2026-09-01", link.GetProperty("fromDate").GetString());
            Assert.StartsWith("2026-09-30", link.GetProperty("toDate").GetString());

            var download = await _client.GetAsync(link.GetProperty("url").GetString());

            Assert.Equal(HttpStatusCode.OK, download.StatusCode);
            Assert.Equal("statement-1000000001-2026-09-01-to-2026-09-30.pdf", download.Content.Headers.ContentDisposition?.FileNameStar);
            Assert.True(IsPdf(await download.Content.ReadAsByteArrayAsync()));
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("{\"fromDate\":\"2026-10-01\",\"toDate\":\"2026-09-30\"}")]
        public async Task StatementDownloadLink_BadDates_Returns400(string body)
        {
            var response = await _client.PostAsync("/api/accounts/1000000001/statements/download-link", new StringContent(body, Encoding.UTF8, "application/json"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Swagger_ListsTheEightEndpoints_WithNoLogin()
        {
            var swagger = await _client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");

            var paths = swagger.GetProperty("paths").EnumerateObject().Select(path => path.Name).OrderBy(path => path, StringComparer.Ordinal);

            Assert.Equal(new[]
            {
                "/api/accounts/{accountId}",
                "/api/accounts/{accountId}/balance",
                "/api/accounts/{accountId}/statements",
                "/api/accounts/{accountId}/statements/download-link",
                "/api/accounts/{accountId}/statements/upload",
                "/api/statements/{statementId}/download",
                "/api/statements/{statementId}/download-link",
                "/health"
            }, paths);
            Assert.False(swagger.GetProperty("components").TryGetProperty("securitySchemes", out _));
        }

        private Task<HttpResponseMessage> Upload(string accountId, string fileName, byte[] content)
        {
            var form = new MultipartFormDataContent { { new ByteArrayContent(content), "File", fileName } };

            return _client.PostAsync($"/api/accounts/{accountId}/statements/upload", form);
        }

        private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
        {
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        private static bool IsPdf(byte[] content)
        {
            return content.AsSpan().StartsWith("%PDF-"u8);
        }
    }
}
