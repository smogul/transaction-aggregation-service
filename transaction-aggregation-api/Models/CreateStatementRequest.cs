using System.ComponentModel.DataAnnotations;

namespace transaction_aggregation_api.Models
{
    public class CreateStatementRequest
    {
        [Required]
        public DateTime? FromDate { get; set; }

        [Required]
        public DateTime? ToDate { get; set; }
    }
}
