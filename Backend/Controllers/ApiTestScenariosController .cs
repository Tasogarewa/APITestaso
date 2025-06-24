using Backend.AppDbContext;
using Backend.DTOs;
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
        public async Task<ActionResult<IEnumerable<ApiTestScenarioDto>>> GetUserScenarios()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var scenarios = await _context.ApiTestScenarios
                .Include(s => s.Tests)
                .Where(s => s.CreatedByUserId == userId)
                .ToListAsync();

            var dtos = scenarios.Select(s => ToDto(s)).ToList();

            return Ok(dtos);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiTestScenarioDto>> GetScenario(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var scenario = await _context.ApiTestScenarios
                .Include(s => s.Tests)
                .FirstOrDefaultAsync(s => s.Id == id && s.CreatedByUserId == userId);

            if (scenario == null)
                return NotFound();

            return Ok(ToDto(scenario));
        }

        [HttpPost]
        public async Task<ActionResult<ApiTestScenarioDto>> CreateScenario([FromBody] ApiTestScenarioDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            
            var tests = await _context.ApiTests
                .Where(t => dto.TestIds.Contains(t.Id) && t.CreatedByUserId == userId)
                .ToListAsync();

            if (tests.Count != dto.TestIds.Count)
                return BadRequest("One or more test IDs are invalid or do not belong to the user.");

            var scenario = new ApiTestScenario
            {
                ScenarioName = dto.ScenarioName,
                CreatedByUserId = userId,
                Tests = tests
            };

            _context.ApiTestScenarios.Add(scenario);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetScenario), new { id = scenario.Id }, ToDto(scenario));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateScenario(int id, [FromBody] ApiTestScenarioDto dto)
        {
            if (id != dto.Id)
                return BadRequest("Id у URL і в тілі запиту не співпадають.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var existing = await _context.ApiTestScenarios
                .Include(s => s.Tests)
                .FirstOrDefaultAsync(s => s.Id == id && s.CreatedByUserId == userId);

            if (existing == null)
                return NotFound();

            
            var tests = await _context.ApiTests
                .Where(t => dto.TestIds.Contains(t.Id) && t.CreatedByUserId == userId)
                .ToListAsync();

            if (tests.Count != dto.TestIds.Count)
                return BadRequest("One or more test IDs are invalid or do not belong to the user.");

            existing.ScenarioName = dto.ScenarioName;
            existing.Tests.Clear();
            existing.Tests = tests;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id:int}")]
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

        private ApiTestScenarioDto ToDto(ApiTestScenario entity)
        {
            return new ApiTestScenarioDto
            {
                Id = entity.Id,
                ScenarioName = entity.ScenarioName,
                CreatedByUserId = entity.CreatedByUserId,
                TestIds = entity.Tests.Select(t => t.Id).ToList()
            };
        }

        private ApiTestScenario ToEntity(ApiTestScenarioDto dto)
        {
            return new ApiTestScenario
            {
                Id = dto.Id,
                ScenarioName = dto.ScenarioName,
                CreatedByUserId = dto.CreatedByUserId,
                Tests = new List<ApiTest>() 
            };
        }
    }

}