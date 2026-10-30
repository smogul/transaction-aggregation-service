namespace transaction_aggregation_api.Security
{
    // Settings for FAPI security, read from the "Fapi" section of appsettings.{Environment}.json.
    //
    // FAPI ("Financial-grade API") is a set of extra-strict OAuth rules that banks use.
    // In its simplest form here it means:
    //   1. every request must carry an access token (a signed pass that says "this app is allowed in"),
    //   2. the token must be signed with PS256, the signing method FAPI asks for,
    //   3. every answer carries an x-fapi-interaction-id so a request can be traced from start to end.
    public class FapiOptions
    {
        public const string PolicyName = "Fapi";
        public const string Scope = "accounts";
        public const string InteractionIdHeader = "x-fapi-interaction-id";

        // true in every environment except Development.
        public bool Enabled { get; set; }

        // Who made the token, and who the token is meant for. Both are checked on every request.
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;

        // The app that is allowed to ask for tokens. Swagger uses these to log in.
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;

        // Example: 10 means a token works for 10 minutes, then you need a new one.
        public int TokenLifetimeMinutes { get; set; } = 10;
    }
}
