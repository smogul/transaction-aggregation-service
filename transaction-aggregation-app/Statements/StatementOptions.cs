namespace transaction_aggregation_app.Statements
{
    // Settings for statements, read from the "Statements" section of appsettings.json.
    public class StatementOptions
    {
        // How long a download link keeps working. Example: 15 means the link stops working after 15 minutes.
        public int LinkLifetimeMinutes { get; set; } = 15;

        // The biggest file a customer may upload. Example: 10 means 10 MB.
        public int MaxUploadSizeMegabytes { get; set; } = 10;
    }
}
