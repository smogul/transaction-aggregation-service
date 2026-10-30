namespace transaction_aggregation_app.Transactions
{
    // One row of a "group by" report. The same shape works for every kind of group.
    // Examples:
    //   Name = "Food"      (grouped by category)
    //   Name = "2026-09"   (grouped by month)
    //   Name = "Excel"     (grouped by data source)
    public class GroupTotalDTO
    {
        public string Name { get; set; } = string.Empty;
        public int TransactionCount { get; set; }
        public decimal MoneyIn { get; set; }
        public decimal MoneyOut { get; set; }
    }
}
