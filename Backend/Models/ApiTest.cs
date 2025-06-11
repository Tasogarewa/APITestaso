using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Backend.Models
{
    public class ApiTest
    {
        public int Id { get; set; }

        public string Name { get; set; } = default!;

        public string Method { get; set; } = default!;

        public string Url { get; set; } = default!;

        public Dictionary<string, string>? Headers { get; set; }

        public object? BodyJson { get; set; }

        public string? ExpectedResponse { get; set; }
        public bool IsMock { get; set; }
        public int? TimeoutSeconds { get; set; }

        public int ExpectedStatusCode { get; set; } = 200;

        [Required]
        public string CreatedByUserId { get; set; } = default!;

        [ForeignKey("CreatedByUserId")]
        public ApplicationUser CreatedByUser { get; set; } = default!;

       
        public Dictionary<string, string>? Save { get; set; }
    }
}
