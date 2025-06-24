using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Security.Claims;

namespace Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TestRunnerController : ControllerBase
    {
        private readonly TestRunnerService _testRunner;

        public TestRunnerController(TestRunnerService testRunner)
        {
            _testRunner = testRunner;
        }

        [HttpPost("RunAll")]
        public async Task<IActionResult> RunAllTests()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var apiResults = await _testRunner.RunAllApiTestsAsync(userId);
            var sqlResults = await _testRunner.RunAllSqlTestsAsync(userId);

            var allResults = apiResults.Concat(sqlResults).ToList();

            return Ok(allResults);
        }

        [HttpPost("RunApiTest/{id}")]
        public async Task<IActionResult> RunApiTestById(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var test = await _testRunner._dbContext.ApiTests
                .FirstOrDefaultAsync(t => t.Id == id && t.CreatedByUserId == userId);

            if (test == null)
                return NotFound();

            var result = await _testRunner
                .ExecuteApiTestAsync(test, userId);

            return Ok(result);
        }

        [HttpPost("RunSqlTest/{id}")]
        public async Task<IActionResult> RunSqlTestById(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var test = await _testRunner._dbContext.SqlTests
                .FirstOrDefaultAsync(t => t.Id == id && t.CreatedByUserId == userId);

            if (test == null)
                return NotFound();

            var result = await _testRunner.RunSingleSqlTestAsync(test, userId);

            return Ok(result);
        }
        [HttpPost("RunApiScenarioById/{scenarioId}")]
        public async Task<IActionResult> RunApiScenarioById(int scenarioId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var scenario = await _testRunner._dbContext.ApiTestScenarios
                .Include(s => s.Tests)
                .FirstOrDefaultAsync(s => s.Id == scenarioId && s.CreatedByUserId == userId);

            if (scenario == null)
                return NotFound("Scenario not found or access denied.");

            if (!scenario.Tests.Any())
                return BadRequest("No tests associated with the scenario.");

            var httpClient = _testRunner._httpClientFactory.CreateClient();
            var results = await _testRunner.ExecuteScenarioAsync(scenario, httpClient, userId);

            return Ok(results);
        }
    }
}