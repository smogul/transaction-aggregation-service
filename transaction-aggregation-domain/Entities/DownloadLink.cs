namespace transaction_aggregation_domain.Entities
{
    public class DownloadLink
    {
        public string Id { get; set; } = string.Empty;
        public string StatementId { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
