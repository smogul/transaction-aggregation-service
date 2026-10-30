using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace transaction_aggregation_api.Security
{
    // A very small token office. It only opens when FAPI is switched on (every environment except Development).
    //
    // An app shows its client id and secret (like a name and password) and gets back an access token.
    // The token is signed with our private key using PS256, so nobody can fake one.
    //
    // Example: POST /connect/token with grant_type=client_credentials, client_id=swagger-client,
    // client_secret=... returns { "access_token": "eyJ...", "token_type": "Bearer", "expires_in": 600 }.
    //
    // In a real bank this job belongs to a separate authorisation server. We keep it here so the
    // whole secure setup can be tried from Swagger on one machine.
    [ApiController]
    [AllowAnonymous]
    [Route("connect/token")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class TokenController : ControllerBase
    {
        private readonly FapiOptions _options;
        private readonly RsaSecurityKey _signingKey;

        public TokenController(FapiOptions options, RsaSecurityKey signingKey)
        {
            _options = options;
            _signingKey = signingKey;
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public IActionResult CreateToken([FromForm] TokenRequest request)
        {
            if (!_options.Enabled)
            {
                return NotFound();
            }

            if (request.GrantType != "client_credentials")
            {
                return BadRequest(new { error = "unsupported_grant_type" });
            }

            var (clientId, clientSecret) = ReadClientCredentials(request);

            if (!IsKnownClient(clientId, clientSecret))
            {
                return Unauthorized(new { error = "invalid_client" });
            }

            var now = DateTime.UtcNow;
            var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
            {
                Issuer = _options.Issuer,
                Audience = _options.Audience,
                IssuedAt = now,
                NotBefore = now,
                Expires = now.AddMinutes(_options.TokenLifetimeMinutes),
                Claims = new Dictionary<string, object>
                {
                    ["sub"] = clientId,
                    ["client_id"] = clientId,
                    ["scope"] = FapiOptions.Scope
                },
                SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.RsaSsaPssSha256)
            });

            return Ok(new TokenResponse
            {
                AccessToken = token,
                ExpiresIn = _options.TokenLifetimeMinutes * 60,
                Scope = FapiOptions.Scope
            });
        }

        // Swagger can send the id and secret in the form body, or in a "Basic" Authorization header
        // (that is "id:secret" turned into Base64). We accept both.
        private (string ClientId, string ClientSecret) ReadClientCredentials(TokenRequest request)
        {
            var header = Request.Headers.Authorization.ToString();

            if (header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = new byte[header.Length];

                // TryFromBase64String says "false" instead of crashing when someone sends rubbish.
                if (Convert.TryFromBase64String(header["Basic ".Length..], bytes, out var length))
                {
                    var parts = Encoding.UTF8.GetString(bytes, 0, length).Split(':', 2);

                    if (parts.Length == 2)
                    {
                        return (Uri.UnescapeDataString(parts[0]), Uri.UnescapeDataString(parts[1]));
                    }
                }
            }

            return (request.ClientId ?? string.Empty, request.ClientSecret ?? string.Empty);
        }

        // FixedTimeEquals takes the same time whether the first letter or the last letter is wrong,
        // so an attacker cannot guess the secret one letter at a time by timing our answers.
        private bool IsKnownClient(string clientId, string clientSecret)
        {
            return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(clientId), Encoding.UTF8.GetBytes(_options.ClientId))
                && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(clientSecret), Encoding.UTF8.GetBytes(_options.ClientSecret));
        }

        public class TokenRequest
        {
            [FromForm(Name = "grant_type")]
            public string? GrantType { get; set; }

            [FromForm(Name = "client_id")]
            public string? ClientId { get; set; }

            [FromForm(Name = "client_secret")]
            public string? ClientSecret { get; set; }

            [FromForm(Name = "scope")]
            public string? Scope { get; set; }
        }

        public class TokenResponse
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; } = string.Empty;

            [JsonPropertyName("token_type")]
            public string TokenType { get; set; } = "Bearer";

            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }

            [JsonPropertyName("scope")]
            public string Scope { get; set; } = string.Empty;
        }
    }
}
