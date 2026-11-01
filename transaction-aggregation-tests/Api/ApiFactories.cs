using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using transaction_aggregation_api;

namespace transaction_aggregation_tests.Api
{
    public class DevelopmentApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:Database", $"Data Source={Guid.NewGuid()};Mode=Memory;Cache=Shared");
        }
    }

    public class FapiApiFactory : WebApplicationFactory<Program>
    {
        public const string Issuer = "https://auth.test/";
        public const string TokenUrl = "https://auth.test/connect/token";
        public const string Audience = "transaction-aggregation-api";

        public static readonly RsaSecurityKey SigningKey = new(RSA.Create(2048)) { KeyId = "test-key" };

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("ConnectionStrings:Database", $"Data Source={Guid.NewGuid()};Mode=Memory;Cache=Shared");
            builder.UseSetting("Fapi:Authority", Issuer);
            builder.UseSetting("Fapi:TokenUrl", TokenUrl);

            builder.ConfigureTestServices(services =>
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    var configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
                    configuration.SigningKeys.Add(SigningKey);

                    options.Configuration = configuration;
                    options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
                }));
        }

        public static string CreateToken(
            string scope = "accounts",
            string audience = Audience,
            string algorithm = SecurityAlgorithms.RsaSsaPssSha256,
            SecurityKey? signingKey = null,
            int expiresInMinutes = 10)
        {
            var now = DateTime.UtcNow;

            return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
            {
                Issuer = Issuer,
                Audience = audience,
                IssuedAt = now.AddMinutes(-30),
                NotBefore = now.AddMinutes(-30),
                Expires = now.AddMinutes(expiresInMinutes),
                Claims = new Dictionary<string, object> { ["scope"] = scope, ["client_id"] = "test-client" },
                SigningCredentials = new SigningCredentials(signingKey ?? SigningKey, algorithm)
            });
        }
    }
}
