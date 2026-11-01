namespace transaction_aggregation_domain.Entities
{
    public class Statement
    {
        public string Id { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public string FileName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
