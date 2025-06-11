namespace Backend.Models
{
    public class SchemaSpec
    {
        public string TableName { get; set; } = default!;
        public List<string> ExpectedColumns { get; set; } = new();
        public List<string> ExpectedIndexes { get; set; } = new();
    }
}
