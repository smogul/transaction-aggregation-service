using System.ComponentModel.DataAnnotations;

namespace transaction_aggregation_api.Models
{
    // What you send to upload a statement file. It is sent as a form, like attaching a file to an email.
    // Example: File = september.docx, FromDate = 2026-09-01, ToDate = 2026-09-30.
    public class UploadStatementRequest
    {
        [Required]
        public IFormFile? File { get; set; }

        [Required]
        public DateTime? FromDate { get; set; }

        [Required]
        public DateTime? ToDate { get; set; }
    }
}
