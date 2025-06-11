namespace Backend.DTOs
{
    public class ScenarioScheduleDto
    {
        public string ScenarioId { get; set; }
        public string UserId { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public string CronExpression { get; set; } 
    }
}
