using Backend.AppDbContext;
using Backend.Models;
using Backend.Services;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace Backend.Quartz
{
    public class RunScenarioJob : IJob
    {
        private readonly TestRunnerService _testRunnerService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApplicationDbContext _dbContext;

        public RunScenarioJob(
            TestRunnerService testRunnerService,
            IHttpClientFactory httpClientFactory,
            ApplicationDbContext dbContext)
        {
            _testRunnerService = testRunnerService;
            _httpClientFactory = httpClientFactory;
            _dbContext = dbContext;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var scenarioId = context.JobDetail.JobDataMap.GetString("ScenarioId");
            var userId = context.JobDetail.JobDataMap.GetString("UserId");

            if (string.IsNullOrEmpty(scenarioId) || string.IsNullOrEmpty(userId))
                return;

            var scenario = await _dbContext.ApiTestScenarios
                .Include(s => s.Tests)
                .FirstOrDefaultAsync(s => s.Id.ToString() == scenarioId);

            if (scenario == null)
                return;

            var client = _httpClientFactory.CreateClient();

            await _testRunnerService.ExecuteScenarioAsync(scenario, client, userId);
        }
    }
}