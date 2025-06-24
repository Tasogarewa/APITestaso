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
    public class SqlTestsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SqlTestsController(ApplicationDbContext context)
        {
            _context = context;
        }

       
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SqlTestDto>>> GetUserSqlTests()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var tests = await _context.SqlTests
                .Where(t => t.CreatedByUserId == userId)
                .ToListAsync();

            return tests.Select(t => ToDto(t)).ToList();
        }

       
        [HttpGet("{id}")]
        public async Task<ActionResult<SqlTestDto>> GetSqlTest(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var test = await _context.SqlTests
                .FirstOrDefaultAsync(t => t.Id == id && t.CreatedByUserId == userId);

            if (test == null)
                return NotFound();

            return ToDto(test);
        }

       
        [HttpPost]
        public async Task<ActionResult<SqlTestDto>> CreateSqlTest([FromBody] SqlTestDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var entity = ToEntity(dto);
            entity.CreatedByUserId = userId;
            entity.CreatedByUser = null!;

            _context.SqlTests.Add(entity);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSqlTest), new { id = entity.Id }, ToDto(entity));
        }

        
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSqlTest(int id, [FromBody] SqlTestDto dto)
        {
            if (id != dto.Id)
                return BadRequest("Id в URL і в тілі запиту не співпадають.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var existing = await _context.SqlTests
                .FirstOrDefaultAsync(t => t.Id == id && t.CreatedByUserId == userId);

            if (existing == null)
                return NotFound();

            existing.Name = dto.Name;
            existing.SqlQuery = dto.SqlQuery;
            existing.TestType = dto.TestType;
            existing.ExpectedJson = dto.ExpectedJson;
            existing.ParametersJson = dto.ParametersJson;
            existing.DatabaseConnectionName = dto.DatabaseConnectionName;

            await _context.SaveChangesAsync();

            return NoContent();
        }

     
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSqlTest(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var existing = await _context.SqlTests
                .FirstOrDefaultAsync(t => t.Id == id && t.CreatedByUserId == userId);

            if (existing == null)
                return NotFound();

            _context.SqlTests.Remove(existing);
            await _context.SaveChangesAsync();

            return NoContent();
        }

    

        private SqlTestDto ToDto(SqlTest entity)
        {
            return new SqlTestDto
            {
                Id = entity.Id,
                Name = entity.Name,
                SqlQuery = entity.SqlQuery,
                TestType = entity.TestType,
                ExpectedJson = entity.ExpectedJson,
                ParametersJson = entity.ParametersJson,
                DatabaseConnectionName = entity.DatabaseConnectionName,
                CreatedByUserId = entity.CreatedByUserId
            };
        }

        private SqlTest ToEntity(SqlTestDto dto)
        {
            return new SqlTest
            {
                Id = dto.Id,
                Name = dto.Name,
                SqlQuery = dto.SqlQuery,
                TestType = dto.TestType,
                ExpectedJson = dto.ExpectedJson,
                ParametersJson = dto.ParametersJson,
                DatabaseConnectionName = dto.DatabaseConnectionName,
                CreatedByUserId = dto.CreatedByUserId
            };
        }
    }
}