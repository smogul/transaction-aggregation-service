namespace transaction_aggregation_app.Statements
{
    public class StatementOptions
    {
        public int LinkLifetimeMinutes { get; set; } = 15;

        public int MaxUploadSizeMegabytes { get; set; } = 10;
    }
}
