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
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var fapiOptions = builder.Configuration.GetSection("Fapi").Get<FapiOptions>() ?? new FapiOptions();

            if (fapiOptions.Enabled && (string.IsNullOrWhiteSpace(fapiOptions.Authority) || string.IsNullOrWhiteSpace(fapiOptions.TokenUrl)))
            {
                throw new InvalidOperationException("FAPI is on, so add Fapi:Authority and Fapi:TokenUrl to appsettings.");
            }

            var statementOptions = builder.Configuration.GetSection("Statements").Get<StatementOptions>() ?? new StatementOptions();
            builder.Services.AddSingleton(fapiOptions);
            builder.Services.AddSingleton(statementOptions);

            var connectionString = builder.Configuration.GetConnectionString("Database")
                ?? throw new InvalidOperationException("Add ConnectionStrings:Database to appsettings.");

            builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
            builder.Services.AddScoped<IAccountRepository, AccountRepository>();
            builder.Services.AddScoped<IStatementRepository, StatementRepository>();

            var excelFilePath = Path.Combine(AppContext.BaseDirectory, builder.Configuration["DataSources:ExcelFilePath"]!);
            var mockBankFilePath = Path.Combine(AppContext.BaseDirectory, builder.Configuration["DataSources:MockBankFilePath"]!);

            builder.Services
                .AddHttpClient<ApiTransactionSource>(client => client.BaseAddress = new Uri(builder.Configuration["DataSources:MockBankApiUrl"]!))
                .ConfigurePrimaryHttpMessageHandler(() => new MockBankApiHandler(mockBankFilePath));

            builder.Services.AddScoped<ITransactionSource, MockAbsaStatementSource>();
            builder.Services.AddScoped<ITransactionSource, MockGoTymeStatementSource>();
            builder.Services.AddScoped<ITransactionSource>(_ => new ExcelTransactionSource(excelFilePath));
            builder.Services.AddScoped<ITransactionSource, DatabaseTransactionSource>();
            builder.Services.AddScoped<ITransactionSource>(services => services.GetRequiredService<ApiTransactionSource>());

            builder.Services.AddSingleton<IStatementPdfGenerator, StatementPdfGenerator>();
            builder.Services.AddSingleton<IStatementFileStore, InMemoryStatementFileStore>();

            builder.Services.AddSingleton<TransactionCategoriser>();
            builder.Services.AddScoped<AccountService>();
            builder.Services.AddScoped<TransactionService>();
            builder.Services.AddScoped<BalanceService>();
            builder.Services.AddScoped<StatementService>();

            builder.Services
                .AddControllers(options =>
                {
                    if (fapiOptions.Enabled)
                    {
                        options.Filters.Add(new AuthorizeFilter(FapiOptions.PolicyName));
                    }
                })
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

                    options.JsonSerializerOptions.Converters.Add(new RandJsonConverter());
                });

            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();

            builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

            if (fapiOptions.Enabled)
            {
                builder.Services
                    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.Authority = fapiOptions.Authority;
                        options.Audience = fapiOptions.Audience;
                        options.MapInboundClaims = false;
                        options.TokenValidationParameters.ValidAlgorithms = new[] { SecurityAlgorithms.RsaSsaPssSha256 };
                    });

                builder.Services.AddAuthorization(options =>
                    options.AddPolicy(FapiOptions.PolicyName, policy => policy
                        .RequireAuthenticatedUser()
                        .RequireAssertion(context => context.User.FindAll("scope").Any(claim => claim.Value.Split(' ').Contains(FapiOptions.Scope)))));
            }

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Transaction Aggregation API",
                    Version = "v1",
                    Description = (fapiOptions.Enabled
                        ? "FAPI is ON. Click Authorize and enter the client id and secret from the authorization server, then try the endpoints. "
                        : "FAPI is OFF in this environment, so no login is needed. ")
                        + "Try the account numbers 1000000001, 1000000002 or 2000000001."
                });

                if (fapiOptions.Enabled)
                {
                    options.AddSecurityDefinition(FapiOptions.PolicyName, new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.OAuth2,
                        Description = "FAPI access token (PS256-signed JWT) from the client credentials flow.",
                        Flows = new OpenApiOAuthFlows
                        {
                            ClientCredentials = new OpenApiOAuthFlow
                            {
                                TokenUrl = new Uri(fapiOptions.TokenUrl),
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

            using var keepDatabaseAlive = new SqliteConnection(connectionString);
            keepDatabaseAlive.Open();

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
