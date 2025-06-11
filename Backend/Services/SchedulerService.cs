using Backend.DTOs;
using Backend.Quartz;
using Quartz;

namespace Backend.Services
{
    public class SchedulerService
    {
        private readonly ISchedulerFactory _schedulerFactory;

        public SchedulerService(ISchedulerFactory schedulerFactory)
        {
            _schedulerFactory = schedulerFactory;
        }
        public async Task ScheduleScenarioAsync(ScenarioScheduleDto schedule)
        {
            var scheduler = await _schedulerFactory.GetScheduler();

            var job = JobBuilder.Create<RunScenarioJob>()
                .WithIdentity($"RunScenarioJob-{schedule.ScenarioId}-{schedule.UserId}")
                .UsingJobData("ScenarioId", schedule.ScenarioId)
                .UsingJobData("UserId", schedule.UserId)
                .Build();

            ITrigger trigger;

            if (!string.IsNullOrEmpty(schedule.CronExpression))
            {
                
                trigger = TriggerBuilder.Create()
                    .WithIdentity($"RunScenarioTrigger-{schedule.ScenarioId}-{schedule.UserId}")
                    .StartAt(schedule.StartTime.UtcDateTime)
                    .WithCronSchedule(schedule.CronExpression)
                    .Build();
            }
            else
            {
                
                trigger = TriggerBuilder.Create()
                    .WithIdentity($"RunScenarioTrigger-{schedule.ScenarioId}-{schedule.UserId}")
                    .StartAt(schedule.StartTime.UtcDateTime)
                    .Build();
            }

            await scheduler.ScheduleJob(job, trigger);
        }
        public async Task<bool> CancelScheduledScenarioAsync(string scenarioId, string userId)
        {
            var scheduler = await _schedulerFactory.GetScheduler();

            var jobKey = new JobKey($"RunScenarioJob-{scenarioId}-{userId}");
            return await scheduler.DeleteJob(jobKey); 
        }
    }

}
