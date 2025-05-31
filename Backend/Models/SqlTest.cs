using Backend.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class SqlTest
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = default!;

    [Required]
    public string SqlQuery { get; set; } = default!;

    public string? ExpectedResult { get; set; }

    [Required]
    public string DatabaseConnectionName { get; set; } = "DefaultConnection";

    [Required]
    public string CreatedByUserId { get; set; } = default!;

    [ForeignKey("CreatedByUserId")]
    public ApplicationUser CreatedByUser { get; set; } = default!;
}