namespace transaction_aggregation_api.Security
{
    public class FapiOptions
    {
        public const string PolicyName = "Fapi";
        public const string Scope = "accounts";
        public const string InteractionIdHeader = "x-fapi-interaction-id";

        public bool Enabled { get; set; }

        public string Authority { get; set; } = string.Empty;
        public string TokenUrl { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;

        public string ClientId { get; set; } = string.Empty;
    }
}
