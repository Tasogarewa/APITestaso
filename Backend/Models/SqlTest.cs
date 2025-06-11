using Backend.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public enum SqlTestType
{
    Scalar,       
    ResultSet,    
    Schema       
}

public class SqlTest
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = default!;

    [Required]
    public string? SqlQuery { get; set; } = default!;

    [Required]
    public SqlTestType TestType { get; set; } = SqlTestType.Scalar;

    public string? ExpectedJson { get; set; }

    public string? ParametersJson { get; set; }

    [Required]
    public string DatabaseConnectionName { get; set; } = "DefaultConnection";

    [Required]
    public string CreatedByUserId { get; set; } = default!;

    [ForeignKey("CreatedByUserId")]
    public ApplicationUser CreatedByUser { get; set; } = default!;
}