using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

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
                .RunAllApiTestsAsync(userId);

            return Ok(result.Where(r => r.ApiTestId == id).FirstOrDefault());
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
    }
}