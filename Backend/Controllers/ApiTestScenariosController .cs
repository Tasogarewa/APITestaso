using Backend.AppDbContext;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ApiTestScenariosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ApiTestScenariosController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ApiTestScenario>>> GetUserScenarios()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var scenarios = await _context.ApiTestScenarios
                .Include(s => s.Tests)
                .Where(s => s.CreatedByUserId == userId)
                .ToListAsync();
            return scenarios;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiTestScenario>> GetScenario(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var scenario = await _context.ApiTestScenarios
                .Include(s => s.Tests)
                .FirstOrDefaultAsync(s => s.Id == id && s.CreatedByUserId == userId);

            if (scenario == null)
                return NotFound();

            return scenario;
        }

        [HttpPost]
        public async Task<ActionResult<ApiTestScenario>> CreateScenario([FromBody] ApiTestScenario scenario)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            scenario.CreatedByUserId = userId;
            scenario.CreatedByUser = null!;

            foreach (var test in scenario.Tests)
            {
                test.CreatedByUserId = userId;
                test.CreatedByUser = null!;
            }

            _context.ApiTestScenarios.Add(scenario);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetScenario), new { id = scenario.Id }, scenario);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateScenario(int id, ApiTestScenario updatedScenario)
        {
            if (id != updatedScenario.Id)
                return BadRequest();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var existing = await _context.ApiTestScenarios
                .Include(s => s.Tests)
                .FirstOrDefaultAsync(s => s.Id == id && s.CreatedByUserId == userId);

            if (existing == null)
                return NotFound();

            
            existing.ScenarioName = updatedScenario.ScenarioName;

           
            _context.ApiTests.RemoveRange(existing.Tests);

          
            foreach (var test in updatedScenario.Tests)
            {
                test.CreatedByUserId = userId;
                test.CreatedByUser = null!;
            }

            existing.Tests = updatedScenario.Tests;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteScenario(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var existing = await _context.ApiTestScenarios
                .Include(s => s.Tests)
                .FirstOrDefaultAsync(s => s.Id == id && s.CreatedByUserId == userId);

            if (existing == null)
                return NotFound();

            _context.ApiTestScenarios.Remove(existing);
            await _context.SaveChangesAsync();
            return NoContent();
        }

    }
}