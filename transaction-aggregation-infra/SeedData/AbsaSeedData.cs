using System.Globalization;
using transaction_aggregation_domain.Entities;
using transaction_aggregation_domain.Enums;

namespace transaction_aggregation_infra.SeedData
{
    public static class AbsaSeedData
    {
        public const string SourceSystem = "AbsaStatements";

        public static List<Transaction> CreateTransactions()
        {
            return new List<Transaction>
            {
                Create("ABSA-001", "1000000001", "2026-08-01", "SALARY ACME HOLDINGS", 25000.00m),
                Create("ABSA-002", "1000000001", "2026-08-02", "CHECKERS SANDTON CITY", -845.30m),
                Create("ABSA-003", "1000000001", "2026-08-03", "UBER TRIP JOHANNESBURG", -132.50m),
                Create("ABSA-004", "1000000001", "2026-08-04", "NETFLIX SUBSCRIPTION", -199.00m),
                Create("ABSA-005", "1000000001", "2026-08-05", "VODACOM CONTRACT", -549.00m),
                Create("ABSA-006", "1000000001", "2026-08-06", "HOME LOAN REPAYMENT", -8500.00m),
                Create("ABSA-007", "1000000001", "2026-08-07", "ENGEN RIVONIA FUEL", -950.00m),
                Create("ABSA-008", "1000000001", "2026-08-08", "CLICKS PHARMACY ROSEBANK", -312.75m),
                Create("ABSA-009", "1000000001", "2026-08-09", "MONTHLY ACCOUNT FEE", -7.50m),
                Create("ABSA-010", "1000000001", "2026-08-10", "PREPAID ELECTRICITY", -600.00m),
                Create("ABSA-011", "1000000001", "2026-08-11", "KFC BRAAMFONTEIN", -89.90m),
                Create("ABSA-012", "1000000001", "2026-08-12", "OUTSURANCE CAR INSURANCE", -1150.00m),
                Create("ABSA-013", "1000000001", "2026-08-14", "TRANSFER TO SAVINGS", -2000.00m),
                Create("ABSA-014", "1000000001", "2026-08-15", "SPOTIFY PREMIUM", -59.99m),
                Create("ABSA-015", "1000000001", "2026-08-16", "WOOLWORTHS FOOD MELROSE", -523.40m),
                Create("ABSA-016", "1000000001", "2026-08-18", "EDGARS STORE ACCOUNT", -450.00m),
                Create("ABSA-017", "1000000001", "2026-08-20", "GAUTRAIN CARD TOP UP", -200.00m),
                Create("ABSA-018", "1000000001", "2026-08-22", "DOCTOR CONSULTATION DR NAIDOO", -650.00m),
                Create("ABSA-019", "1000000001", "2026-08-25", "CASH DEPOSIT BRANCH", 500.00m),
                Create("ABSA-020", "1000000001", "2026-08-28", "PICK N PAY HYDE PARK", -310.20m),
                Create("ABSA-021", "1000000002", "2026-08-14", "TRANSFER FROM CHEQUE", 2000.00m),
                Create("ABSA-022", "1000000002", "2026-08-29", "INTEREST EARNED", 85.40m),
                Create("ABSA-023", "2000000001", "2026-08-01", "SALARY BRIGHT FUTURES NPO", 14500.00m),
                Create("ABSA-024", "2000000001", "2026-08-05", "MTN AIRTIME", -100.00m),
                Create("ABSA-025", "2000000001", "2026-08-09", "RENT PAYMENT FLAT 12", -5500.00m)
            };
        }

        private static Transaction Create(string id, string accountId, string date, string description, decimal amount)
        {
            return new Transaction
            {
                Id = id,
                AccountId = accountId,
                Amount = amount,
                Currency = Currency.ZAR,
                TransactionDate = DateTime.Parse(date, CultureInfo.InvariantCulture),
                Description = description,
                Category = Categories.Uncategorized,
                SourceSystem = SourceSystem
            };
        }
    }
}
