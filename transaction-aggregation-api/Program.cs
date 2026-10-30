using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using transaction_aggregation_api.Errors;
using transaction_aggregation_api.Json;
using transaction_aggregation_api.Security;
using transaction_aggregation_app.Accounts;
using transaction_aggregation_app.Balances;
using transaction_aggregation_app.Statements;
using transaction_aggregation_app.Transactions;
using transaction_aggregation_domain.Interfaces;
using transaction_aggregation_infra.DataAccess;
using transaction_aggregation_infra.Statements;

namespace transaction_aggregation_api
{
    // The starting point of the app.
    // This is the only file that knows about every project. It connects each interface
    // (for example IStatementFileStore) to the class that does the real work (InMemoryStatementFileStore).
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Settings come from appsettings.json plus appsettings.{Environment}.json.
            // Example: when ASPNETCORE_ENVIRONMENT is "Development", appsettings.Development.json switches FAPI off.
            var fapiOptions = builder.Configuration.GetSection("Fapi").Get<FapiOptions>() ?? new FapiOptions();
            var statementOptions = builder.Configuration.GetSection("Statements").Get<StatementOptions>() ?? new StatementOptions();
            builder.Services.AddSingleton(fapiOptions);
            builder.Services.AddSingleton(statementOptions);

            // Step 1: the database. Where it lives is set by ConnectionStrings:Database in appsettings.
            var connectionString = builder.Configuration.GetConnectionString("Database")
                ?? throw new InvalidOperationException("Add ConnectionStrings:Database to appsettings.");

            builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
            builder.Services.AddScoped<IAccountRepository, AccountRepository>();
            builder.Services.AddScoped<IStatementRepository, StatementRepository>();

            // Step 2: the five data sources. TransactionService asks every one of them for transactions.
            // All the mock data lives in the SeedData folder, which is copied next to the app.
            var excelFilePath = Path.Combine(AppContext.BaseDirectory, builder.Configuration["DataSources:ExcelFilePath"]!);
            var mockBankFilePath = Path.Combine(AppContext.BaseDirectory, builder.Configuration["DataSources:MockBankFilePath"]!);

            builder.Services
                .AddHttpClient<ApiTransactionSource>(client => client.BaseAddress = new Uri(builder.Configuration["DataSources:MockBankApiUrl"]!))
                // MockBankApiHandler answers the call itself, so the request never really leaves the app.
                .ConfigurePrimaryHttpMessageHandler(() => new MockBankApiHandler(mockBankFilePath));

            builder.Services.AddScoped<ITransactionSource, MockAbsaStatementSource>();
            builder.Services.AddScoped<ITransactionSource, MockGoTymeStatementSource>();
            builder.Services.AddScoped<ITransactionSource>(_ => new ExcelTransactionSource(excelFilePath));
            builder.Services.AddScoped<ITransactionSource, DatabaseTransactionSource>();
            builder.Services.AddScoped<ITransactionSource>(services => services.GetRequiredService<ApiTransactionSource>());

            // Step 3: making and keeping PDF statements.
            // The file store is a singleton (one for the whole app), so every request sees the same PDFs.
            builder.Services.AddSingleton<IStatementPdfGenerator, StatementPdfGenerator>();
            builder.Services.AddSingleton<IStatementFileStore, InMemoryStatementFileStore>();

            // Step 4: our modules.
            builder.Services.AddSingleton<TransactionCategoriser>();
            builder.Services.AddScoped<AccountService>();
            builder.Services.AddScoped<TransactionService>();
            builder.Services.AddScoped<BalanceService>();
            builder.Services.AddScoped<StatementService>();

            // Step 5: controllers, error handling and the health check.
            builder.Services
                .AddControllers(options =>
                {
                    // With FAPI on, every endpoint needs a valid token unless it is marked [AllowAnonymous].
                    if (fapiOptions.Enabled)
                    {
                        options.Filters.Add(new AuthorizeFilter(FapiOptions.PolicyName));
                    }
                })
                .AddJsonOptions(options =>
                {
                    // Show enums as words ("Food") instead of numbers (3).
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

                    // Show money as rands with two decimals (14500.00) everywhere.
                    options.JsonSerializerOptions.Converters.Add(new RandJsonConverter());
                });

            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();

            // The health check tries to talk to the database. See HealthController.
            builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

            // Step 6: FAPI security. Switched on in every environment except Development.
            // The key that signs tokens is made fresh each time the app starts, so it never sits in a file.
            // A real bank would keep it in a key vault.
            var signingKey = new RsaSecurityKey(RSA.Create(2048));
            builder.Services.AddSingleton(signingKey);

            if (fapiOptions.Enabled)
            {
                builder.Services
                    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        // Keep claim names exactly as they are in the token, e.g. "scope" stays "scope".
                        options.MapInboundClaims = false;
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidIssuer = fapiOptions.Issuer,
                            ValidAudience = fapiOptions.Audience,
                            IssuerSigningKey = signingKey,
                            ValidAlgorithms = new[] { SecurityAlgorithms.RsaSsaPssSha256 }
                        };
                    });

                builder.Services.AddAuthorization(options =>
                    options.AddPolicy(FapiOptions.PolicyName, policy => policy
                        .RequireAuthenticatedUser()
                        .RequireClaim("scope", FapiOptions.Scope)));
            }

            // Step 7: Swagger, the web page where you can try every endpoint.
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Transaction Aggregation API",
                    Version = "v1",
                    Description = (fapiOptions.Enabled
                        ? "FAPI is ON. Click Authorize, type the client secret from appsettings, then try the endpoints. "
                        : "FAPI is OFF in this environment, so no login is needed. ")
                        + "Try the account numbers 1000000001, 1000000002 or 2000000001."
                });

                // Use the /// comments on the controllers as descriptions in Swagger.
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));

                if (fapiOptions.Enabled)
                {
                    // Tell Swagger how to get a token: the OAuth "client credentials" flow at /connect/token.
                    options.AddSecurityDefinition(FapiOptions.PolicyName, new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.OAuth2,
                        Description = "FAPI access token (PS256-signed JWT) from the client credentials flow.",
                        Flows = new OpenApiOAuthFlows
                        {
                            ClientCredentials = new OpenApiOAuthFlow
                            {
                                TokenUrl = new Uri("/connect/token", UriKind.Relative),
                                Scopes = new Dictionary<string, string>
                                {
                                    [FapiOptions.Scope] = "Read accounts, balances, transactions and statements"
                                }
                            }
                        }
                    });

                    options.OperationFilter<FapiSwaggerOperationFilter>();
                }
            });

            var app = builder.Build();

            // An in-memory database is thrown away as soon as its last connection closes.
            // So we open one connection now and keep it open until the app stops.
            // (If the connection string points at a file instead, this does no harm.)
            using var keepDatabaseAlive = new SqliteConnection(connectionString);
            keepDatabaseAlive.Open();

            // Make the tables and fill them with the seed data.
            using (var scope = app.Services.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
            }

            app.UseExceptionHandler();

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                if (fapiOptions.Enabled)
                {
                    options.OAuthClientId(fapiOptions.ClientId);
                    options.OAuthScopes(FapiOptions.Scope);
                }
            });

            if (fapiOptions.Enabled)
            {
                // FAPI rule: every answer carries an x-fapi-interaction-id so a request can be traced.
                // If the caller sends one, we send the same one back. If not, we make a new one.
                app.Use(async (context, next) =>
                {
                    var interactionId = context.Request.Headers[FapiOptions.InteractionIdHeader].FirstOrDefault();

                    if (string.IsNullOrWhiteSpace(interactionId))
                    {
                        interactionId = Guid.NewGuid().ToString();
                    }

                    context.Response.OnStarting(() =>
                    {
                        context.Response.Headers[FapiOptions.InteractionIdHeader] = interactionId;
                        return Task.CompletedTask;
                    });

                    await next();
                });

                app.UseAuthentication();
                app.UseAuthorization();
            }

            app.MapControllers();

            app.Run();
        }
    }
}
