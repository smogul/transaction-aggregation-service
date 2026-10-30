using System.ComponentModel.DataAnnotations;

namespace transaction_aggregation_api.Models
{
    // What you send to ask for a new statement.
    // Example: { "fromDate": "2026-09-01", "toDate": "2026-09-30" } means "September, please".
    public class CreateStatementRequest
    {
        [Required]
        public DateTime? FromDate { get; set; }

        [Required]
        public DateTime? ToDate { get; set; }
    }
}
