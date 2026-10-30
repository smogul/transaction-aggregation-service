using System.Globalization;
using transaction_aggregation_domain.Entities;
using transaction_aggregation_domain.Enums;

namespace transaction_aggregation_infra.SeedData
{
    // The starting rows for the database (data source 4 of 5).
    // They are written into the tables when the app starts and the database is still empty.
    // The database lives in memory, so every restart begins again from these rows.
    public static class DatabaseSeedData
    {
        public const string SourceSystem = "CapitecDatabase";

        // Two customers and three accounts.
        // CUST-001 has an everyday account and a savings account. CUST-002 has one everyday account.
        public static readonly Account[] Accounts =
        {
            new Account { Id = "1000000001", CustomerId = "CUST-001", AccountName = "Everyday Account", Currency = Currency.ZAR, OpeningBalance = 5000.00m },
            new Account { Id = "1000000002", CustomerId = "CUST-001", AccountName = "Savings Account", Currency = Currency.ZAR, OpeningBalance = 20000.00m },
            new Account { Id = "2000000001", CustomerId = "CUST-002", AccountName = "Everyday Account", Currency = Currency.ZAR, OpeningBalance = 3000.00m }
        };

        // 25 transactions from July 2026. They all start Uncategorized, the categoriser labels them later.
        public static readonly Transaction[] Transactions =
        {
            Create("DB-001", "1000000001", "2026-07-01", "SALARY ACME HOLDINGS", 25000.00m),
            Create("DB-002", "1000000001", "2026-07-02", "CHECKERS SANDTON CITY", -790.10m),
            Create("DB-003", "1000000001", "2026-07-04", "UBER TRIP ROSEBANK", -110.00m),
            Create("DB-004", "1000000001", "2026-07-04", "NETFLIX SUBSCRIPTION", -199.00m),
            Create("DB-005", "1000000001", "2026-07-05", "VODACOM CONTRACT", -549.00m),
            Create("DB-006", "1000000001", "2026-07-06", "HOME LOAN REPAYMENT", -8500.00m),
            Create("DB-007", "1000000001", "2026-07-08", "ENGEN RIVONIA FUEL", -905.00m),
            Create("DB-008", "1000000001", "2026-07-09", "MONTHLY ACCOUNT FEE", -7.50m),
            Create("DB-009", "1000000001", "2026-07-10", "PREPAID ELECTRICITY", -600.00m),
            Create("DB-010", "1000000001", "2026-07-11", "OUTSURANCE CAR INSURANCE", -1150.00m),
            Create("DB-011", "1000000001", "2026-07-13", "DISCOVERY HEALTH MEDICAL AID", -3200.00m),
            Create("DB-012", "1000000001", "2026-07-14", "TRANSFER TO SAVINGS", -2000.00m),
            Create("DB-013", "1000000001", "2026-07-16", "SPAR NORTHCLIFF", -276.45m),
            Create("DB-014", "1000000001", "2026-07-18", "EDGARS STORE ACCOUNT", -450.00m),
            Create("DB-015", "1000000001", "2026-07-20", "PAYSHAP TO T MOKOENA", -300.00m),
            Create("DB-016", "1000000001", "2026-07-22", "RAIN 5G DATA", -479.00m),
            Create("DB-017", "1000000001", "2026-07-25", "SNAPSCAN 8812 MARKET", -120.00m),
            Create("DB-018", "1000000001", "2026-07-28", "CREDIT CARD PAYMENT", -3000.00m),
            Create("DB-019", "1000000002", "2026-07-14", "TRANSFER FROM CHEQUE", 2000.00m),
            Create("DB-020", "1000000002", "2026-07-31", "INTEREST EARNED", 80.15m),
            Create("DB-021", "2000000001", "2026-07-01", "SALARY BRIGHT FUTURES NPO", 14500.00m),
            Create("DB-022", "2000000001", "2026-07-03", "SHOPRITE SOWETO", -512.30m),
            Create("DB-023", "2000000001", "2026-07-09", "RENT PAYMENT FLAT 12", -5500.00m),
            Create("DB-024", "2000000001", "2026-07-12", "TAXI FARE", -350.00m),
            Create("DB-025", "2000000001", "2026-07-20", "STUDENT LOAN REPAYMENT", -1200.00m)
        };

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
