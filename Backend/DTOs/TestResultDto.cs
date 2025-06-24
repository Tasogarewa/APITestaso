namespace Backend.DTOs
{
    public class TestResultDto
    {
        public DateTime ExecutedAt { get; set; }
        public bool IsSuccess { get; set; }
        public string? Response { get; set; }
        public string? ErrorMessage { get; set; }
        public int? ApiTestId { get; set; }
        public long? DurationMilliseconds { get; set; }
        public int? SqlTestId { get; set; }
        public string ExecutedByUserId { get; set; } = string.Empty;
    }
}
