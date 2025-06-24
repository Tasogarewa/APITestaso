namespace Backend.DTOs
{
    public class TestStatsDto
    {
        public int TotalTests { get; set; }
        public int TotalSuccess { get; set; }
        public int TotalFailed { get; set; }

        public Dictionary<string, TestTypeStats> ResultsByType { get; set; } = new();
        public List<ExecutionPoint> ExecutionTrend { get; set; } = new();
    }

    public class TestTypeStats
    {
        public int Total { get; set; }
        public int Success { get; set; }
        public int Failed { get; set; }
    }

    public class ExecutionPoint
    {
        public string Label { get; set; } = string.Empty;
        public long? Duration { get; set; }
    }
}
