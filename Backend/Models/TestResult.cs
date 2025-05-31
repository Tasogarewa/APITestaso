using Backend.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class TestResult
{
    public int Id { get; set; }

    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    public bool IsSuccess { get; set; }

    public string? Response { get; set; }

    public string? ErrorMessage { get; set; }

    public int? ApiTestId { get; set; }
    public ApiTest? ApiTest { get; set; }

    public int? SqlTestId { get; set; }
    public SqlTest? SqlTest { get; set; }

    [Required]
    public string ExecutedByUserId { get; set; } = default!;

    [ForeignKey("ExecutedByUserId")]
    public ApplicationUser ExecutedByUser { get; set; } = default!;
}