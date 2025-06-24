namespace Backend.DTOs
{
    public class SqlTestDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public string SqlQuery { get; set; } = string.Empty;

        public SqlTestType TestType { get; set; } = SqlTestType.Scalar;

        public string? ExpectedJson { get; set; }

        public string? ParametersJson { get; set; }

        public string DatabaseConnectionName { get; set; } = "DefaultConnection";

        public string CreatedByUserId { get; set; } = string.Empty;
    }
}
