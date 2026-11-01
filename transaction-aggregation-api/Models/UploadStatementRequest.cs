using System.ComponentModel.DataAnnotations;

namespace transaction_aggregation_api.Models
{
    public class UploadStatementRequest
    {
        [Required]
        public IFormFile? File { get; set; }
    }
}
