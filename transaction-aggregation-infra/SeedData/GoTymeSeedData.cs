using System.Globalization;
using transaction_aggregation_domain.Entities;
using transaction_aggregation_domain.Enums;

namespace transaction_aggregation_infra.SeedData
{
    // Pretend these 25 transactions from September 2026 came from GoTyme (data source 2 of 5).
    // They are written straight into the code, so no file, database or internet connection is needed.
    public static class GoTymeSeedData
    {
        public const string SourceSystem = "GoTymeStatements";

        // We build brand new objects every time we are asked, so when the categoriser writes a category
        // onto a transaction, the seed rows here stay exactly as they are.
        public static List<Transaction> CreateTransactions()
        {
            return new List<Transaction>
            {
                Create("GOTYME-001", "1000000001", "2026-09-01", "SALARY ACME HOLDINGS", 25000.00m),
                Create("GOTYME-002", "1000000001", "2026-09-02", "PICK N PAY RANDBURG", -912.60m),
                Create("GOTYME-003", "1000000001", "2026-09-03", "BOLT RIDE SANDTON", -98.00m),
                Create("GOTYME-004", "1000000001", "2026-09-04", "SHOWMAX MONTHLY", -99.00m),
                Create("GOTYME-005", "1000000001", "2026-09-05", "TELKOM FIBRE", -699.00m),
                Create("GOTYME-006", "1000000001", "2026-09-06", "HOME LOAN REPAYMENT", -8500.00m),
                Create("GOTYME-007", "1000000001", "2026-09-07", "SHELL ULTRA CITY FUEL", -1020.00m),
                Create("GOTYME-008", "1000000001", "2026-09-09", "ATM WITHDRAWAL FEE", -10.00m),
                Create("GOTYME-009", "1000000001", "2026-09-10", "CITY OF JOHANNESBURG RATES", -1450.00m),
                Create("GOTYME-010", "1000000001", "2026-09-12", "NANDOS ROSEBANK", -245.00m),
                Create("GOTYME-011", "1000000001", "2026-09-13", "DISCOVERY HEALTH MEDICAL AID", -3200.00m),
                Create("GOTYME-012", "1000000001", "2026-09-14", "TRANSFER TO SAVINGS", -2000.00m),
                Create("GOTYME-013", "1000000001", "2026-09-15", "STER KINEKOR MOVIES", -180.00m),
                Create("GOTYME-014", "1000000001", "2026-09-17", "UBER EATS ORDER", -215.50m),
                Create("GOTYME-015", "1000000001", "2026-09-19", "TRUWORTHS ACCOUNT", -380.00m),
                Create("GOTYME-016", "1000000001", "2026-09-21", "VEHICLE FINANCE WESBANK", -4200.00m),
                Create("GOTYME-017", "1000000001", "2026-09-24", "PAYPAL *ONLINESTORE", -349.99m),
                Create("GOTYME-018", "1000000001", "2026-09-26", "BUILDERS WAREHOUSE", -1275.00m),
                Create("GOTYME-019", "1000000001", "2026-09-29", "SANLAM LIFE INSURANCE", -890.00m),
                Create("GOTYME-020", "1000000002", "2026-09-14", "TRANSFER FROM CHEQUE", 2000.00m),
                Create("GOTYME-021", "1000000002", "2026-09-30", "INTEREST EARNED", 92.10m),
                Create("GOTYME-022", "2000000001", "2026-09-01", "SALARY BRIGHT FUTURES NPO", 14500.00m),
                Create("GOTYME-023", "2000000001", "2026-09-03", "SPAR KILLARNEY", -432.15m),
                Create("GOTYME-024", "2000000001", "2026-09-09", "RENT PAYMENT FLAT 12", -5500.00m),
                Create("GOTYME-025", "2000000001", "2026-09-18", "CELL C DATA BUNDLE", -149.00m)
            };
        }

        // A small helper so each row above fits on one line.
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
