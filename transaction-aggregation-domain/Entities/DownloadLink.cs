namespace transaction_aggregation_domain.Entities
{
    // A DownloadLink works like a movie ticket for one statement.
    // Whoever holds the ticket can download the PDF, but only until the ticket expires.
    //
    // We never save the ticket itself, only a scrambled copy of it (a "hash").
    // So even if someone stole our database, they could not use the links inside it.
    public class DownloadLink
    {
        public string Id { get; set; } = string.Empty;
        public string StatementId { get; set; } = string.Empty;
        public string TokenHash { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
