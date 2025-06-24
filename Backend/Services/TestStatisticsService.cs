using Backend.DTOs;

namespace Backend.Services
{
    public class TestStatisticsService 
    {
        public TestStatsDto ComputeStatistics(List<TestResult> results)
        {
            var dto = new TestStatsDto
            {
                TotalTests = results.Count,
                TotalSuccess = results.Count(r => r.IsSuccess),
                TotalFailed = results.Count(r => !r.IsSuccess)
            };

            
            var grouped = results.GroupBy(r =>
            {
                if (r.ApiTestId.HasValue) return "API";
                if (r.SqlTestId.HasValue) return "SQL";
                return "Unknown";
            });

            foreach (var group in grouped)
            {
                var stats = new TestTypeStats
                {
                    Total = group.Count(),
                    Success = group.Count(r => r.IsSuccess),
                    Failed = group.Count(r => !r.IsSuccess)
                };

                dto.ResultsByType[group.Key] = stats;
            }

            
            dto.ExecutionTrend = results
                .OrderBy(r => r.ExecutedAt)
                .Select((r, index) => new ExecutionPoint
                {
                    Label = $"#{index + 1} {r.ExecutedAt:HH:mm:ss}",
                    Duration = r.DurationMilliseconds
                }).ToList();

            return dto;
        }
    }
}
