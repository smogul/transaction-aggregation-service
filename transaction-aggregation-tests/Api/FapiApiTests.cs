using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace transaction_aggregation_tests.Api
{
    public class FapiApiTests : IClassFixture<FapiApiFactory>
    {
        private const string InteractionIdHeader = "x-fapi-interaction-id";

        private readonly HttpClient _client;

        public FapiApiTests(FapiApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Health_NeedsNoToken()
        {
            Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/health")).StatusCode);
        }

        [Theory]
        [InlineData("/api/accounts/1000000001")]
        [InlineData("/api/accounts/1000000001/balance")]
        [InlineData("/api/accounts/1000000001/statements")]
        [InlineData("/api/statements/3f2b6c1e-8d4a-4c3b-9e51-2a7d0f6b1c9e/download")]
        public async Task NoToken_Returns401_WithANewInteractionId(string url)
        {
            var response = await _client.GetAsync(url);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.True(Guid.TryParse(response.Headers.GetValues(InteractionIdHeader).Single(), out _));
        }

        [Fact]
        public async Task ValidToken_Returns200_AndSendsBackTheCallersInteractionId()
        {
            var request = Request(HttpMethod.Get, "/api/accounts/1000000001", FapiApiFactory.CreateToken());
            request.Headers.Add(InteractionIdHeader, "93bac548-d2de-4546-b106-880a5018460d");

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("93bac548-d2de-4546-b106-880a5018460d", response.Headers.GetValues(InteractionIdHeader).Single());
        }

        [Fact]
        public async Task TokenWithSeveralScopes_IncludingAccounts_Returns200()
        {
            var response = await _client.SendAsync(Request(HttpMethod.Get, "/api/accounts/1000000001/balance", FapiApiFactory.CreateToken(scope: "openid accounts")));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task TokenWithoutTheAccountsScope_Returns403()
        {
            var response = await _client.SendAsync(Request(HttpMethod.Get, "/api/accounts/1000000001", FapiApiFactory.CreateToken(scope: "openid")));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task TokenSignedWithRs256InsteadOfPs256_Returns401()
        {
            var token = FapiApiFactory.CreateToken(algorithm: SecurityAlgorithms.RsaSha256);

            Assert.Equal(HttpStatusCode.Unauthorized, (await _client.SendAsync(Request(HttpMethod.Get, "/api/accounts/1000000001", token))).StatusCode);
        }

        [Fact]
        public async Task TokenMadeForAnotherApi_Returns401()
        {
            var token = FapiApiFactory.CreateToken(audience: "another-api");

            Assert.Equal(HttpStatusCode.Unauthorized, (await _client.SendAsync(Request(HttpMethod.Get, "/api/accounts/1000000001", token))).StatusCode);
        }

        [Fact]
        public async Task ExpiredToken_Returns401()
        {
            var token = FapiApiFactory.CreateToken(expiresInMinutes: -10);

            Assert.Equal(HttpStatusCode.Unauthorized, (await _client.SendAsync(Request(HttpMethod.Get, "/api/accounts/1000000001", token))).StatusCode);
        }

        [Fact]
        public async Task TokenSignedWithSomeoneElsesKey_Returns401()
        {
            var token = FapiApiFactory.CreateToken(signingKey: new RsaSecurityKey(RSA.Create(2048)));

            Assert.Equal(HttpStatusCode.Unauthorized, (await _client.SendAsync(Request(HttpMethod.Get, "/api/accounts/1000000001", token))).StatusCode);
        }

        [Fact]
        public async Task StatementDownloadLink_ForAPeriod_NeedsAToken()
        {
            const string url = "/api/accounts/1000000001/statements/download-link?fromDate=2026-09-01&toDate=2026-09-30";

            Assert.Equal(HttpStatusCode.Unauthorized, (await _client.PostAsync(url, null)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(Request(HttpMethod.Post, url, FapiApiFactory.CreateToken()))).StatusCode);
        }

        [Fact]
        public async Task WithAToken_UploadLinkAndDownloadAllWork()
        {
            var token = FapiApiFactory.CreateToken();

            var upload = Request(HttpMethod.Post, "/api/accounts/1000000001/statements/upload", token);
            upload.Content = new MultipartFormDataContent { { new ByteArrayContent(Encoding.ASCII.GetBytes("hello")), "File", "note.txt" } };
            var uploaded = await _client.SendAsync(upload);
            Assert.Equal(HttpStatusCode.Created, uploaded.StatusCode);

            var statementId = (await uploaded.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();

            var link = await _client.SendAsync(Request(HttpMethod.Post, $"/api/statements/{statementId}/download-link", token));
            Assert.Equal(HttpStatusCode.OK, link.StatusCode);

            Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync($"/api/statements/{statementId}/download")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(Request(HttpMethod.Get, $"/api/statements/{statementId}/download", token))).StatusCode);
        }

        [Fact]
        public async Task Swagger_ShowsTheLoginOnEveryEndpointExceptHealth()
        {
            var swagger = await _client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");

            var tokenUrl = swagger.GetProperty("components").GetProperty("securitySchemes").GetProperty("Fapi")
                .GetProperty("flows").GetProperty("clientCredentials").GetProperty("tokenUrl").GetString();
            Assert.Equal(FapiApiFactory.TokenUrl, tokenUrl);

            foreach (var path in swagger.GetProperty("paths").EnumerateObject())
            {
                foreach (var operation in path.Value.EnumerateObject())
                {
                    var hasLogin = operation.Value.TryGetProperty("security", out _);
                    Assert.Equal(path.Name != "/health", hasLogin);
                }
            }
        }

        private static HttpRequestMessage Request(HttpMethod method, string url, string token)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return request;
        }
    }
}
