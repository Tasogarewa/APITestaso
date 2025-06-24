using Backend.DTOs;
using Backend.Quartz;
using Quartz;
using Quartz.Impl.Matchers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"RunScenarioTrigger-{schedule.ScenarioId}-{schedule.UserId}")
                .StartAt(schedule.StartTime.UtcDateTime)
                .WithCronSchedule(schedule.CronExpression)
                .Build();

            await scheduler.ScheduleJob(job, trigger);
        }

        public async Task<bool> CancelScheduledScenarioAsync(string scenarioId, string userId)
        {
            var scheduler = await _schedulerFactory.GetScheduler();

            var jobKey = new JobKey($"RunScenarioJob-{scenarioId}-{userId}");
            return await scheduler.DeleteJob(jobKey);
        }

        public async Task<List<ScenarioScheduleDto>> GetAllScheduledScenariosAsync()
        {
            var scheduler = await _schedulerFactory.GetScheduler();

            
            var jobGroupNames = await scheduler.GetJobGroupNames();
            var scheduledScenarios = new List<ScenarioScheduleDto>();

            foreach (var group in jobGroupNames)
            {
                var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(group));

                foreach (var jobKey in jobKeys)
                {
                    
                    if (!jobKey.Name.StartsWith("RunScenarioJob-"))
                        continue;

                    var jobDetail = await scheduler.GetJobDetail(jobKey);

                    var dataMap = jobDetail.JobDataMap;

                    var scenarioId = dataMap.GetString("ScenarioId");
                    var userId = dataMap.GetString("UserId");

                   
                    var triggers = await scheduler.GetTriggersOfJob(jobKey);
                    var trigger = triggers.FirstOrDefault();

                    if (trigger == null)
                        continue;

                    var startTimeUtc = trigger.GetNextFireTimeUtc()?.DateTime ?? trigger.StartTimeUtc.DateTime;
                    var cronTrigger = trigger as ICronTrigger;

                    var cronExpression = cronTrigger?.CronExpressionString;

                    var scheduledDto = new ScenarioScheduleDto
                    {
                        ScenarioId = scenarioId,
                        UserId = userId,
                        StartTime = startTimeUtc,
                        CronExpression = cronExpression
                    };

                    scheduledScenarios.Add(scheduledDto);
                }
            }

            return scheduledScenarios;
        }
    }
}