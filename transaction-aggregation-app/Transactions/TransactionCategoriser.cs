using transaction_aggregation_domain.Enums;

namespace transaction_aggregation_app.Transactions
{
    // Decides which category a transaction belongs in by looking for keywords in its description.
    // It is like sorting sweets into jars by reading the wrapper.
    //
    // Example: "CHECKERS SANDTON CITY" contains "CHECKERS", so it goes into the Food jar.
    // Example: "PAYPAL *ONLINESTORE" matches nothing, so it stays Uncategorized.
    public class TransactionCategoriser
    {
        // The rules are checked from top to bottom and the first match wins.
        // Order matters: "UBER EATS" must be checked (Food) before "UBER TRIP" (Transport) gets a chance.
        private static readonly List<(Categories Category, string[] Keywords)> Rules = new()
        {
            (Categories.Income, new[] { "SALARY", "INTEREST EARNED" }),
            (Categories.Transfer, new[] { "TRANSFER", "PAYSHAP" }),
            (Categories.Loans, new[] { "LOAN", "VEHICLE FINANCE" }),
            (Categories.Accounts, new[] { "STORE ACCOUNT", "EDGARS", "TRUWORTHS", "CREDIT CARD PAYMENT" }),
            (Categories.Insurance, new[] { "INSURANCE", "OUTSURANCE", "OLD MUTUAL", "SANLAM" }),
            (Categories.Medical, new[] { "MEDICAL AID", "PHARMACY", "CLICKS", "DIS-CHEM", "DOCTOR", "DENTIST" }),
            (Categories.Food, new[] { "CHECKERS", "PICK N PAY", "WOOLWORTHS", "SPAR", "SHOPRITE", "KFC", "NANDOS", "UBER EATS", "MR D" }),
            (Categories.Transport, new[] { "UBER TRIP", "BOLT", "ENGEN", "SHELL", "SASOL", "GAUTRAIN", "TAXI" }),
            (Categories.Entertainment, new[] { "NETFLIX", "SHOWMAX", "SPOTIFY", "DSTV", "STER KINEKOR", "COMPUTICKET" }),
            (Categories.Communication, new[] { "VODACOM", "MTN", "TELKOM", "CELL C", "RAIN 5G", "AIRTIME", "DATA BUNDLE" }),
            (Categories.Household, new[] { "ELECTRICITY", "RATES", "RENT PAYMENT", "BUILDERS" }),
            (Categories.Fees, new[] { "FEE", "BANK CHARGES" })
        };

        public Categories Categorise(string description)
        {
            foreach (var rule in Rules)
            {
                if (rule.Keywords.Any(keyword => description.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                {
                    return rule.Category;
                }
            }

            return Categories.Uncategorized;
        }
    }
}
