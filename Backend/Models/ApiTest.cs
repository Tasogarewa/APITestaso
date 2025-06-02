using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    public class ApiTest
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string Method { get; set; } = default!; 
        public string Url { get; set; } = default!;
        public string? HeadersJson { get; set; } 
        public string? Body { get; set; }
        public string? ExpectedResponse { get; set; }
        public int? TimeoutSeconds { get; set; }
        public int ExpectedStatusCode { get; set; } = 200;
        [Required]
        public string CreatedByUserId { get; set; } = default!;
        [ForeignKey("CreatedByUserId")]
        public ApplicationUser CreatedByUser { get; set; } = default!;
    }
}
