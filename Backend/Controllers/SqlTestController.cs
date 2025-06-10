using Backend.AppDbContext;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        public async Task<ActionResult<IEnumerable<SqlTest>>> GetUserSqlTests()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return await _context.SqlTests
                .Where(t => t.CreatedByUserId == userId)
                .ToListAsync();
        }

        
        [HttpGet("{id}")]
        public async Task<ActionResult<SqlTest>> GetSqlTest(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var test = await _context.SqlTests
                .FirstOrDefaultAsync(t => t.Id == id && t.CreatedByUserId == userId);

            if (test == null)
                return NotFound();

            return test;
        }

        [HttpPost]
        public async Task<ActionResult<SqlTest>> CreateSqlTest(SqlTest sqlTest)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            sqlTest.CreatedByUserId = userId;
            sqlTest.CreatedByUser = null!;
            _context.SqlTests.Add(sqlTest);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetSqlTest), new { id = sqlTest.Id }, sqlTest);
        }

       
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSqlTest(int id, SqlTest updatedTest)
        {
            if (id != updatedTest.Id)
                return BadRequest();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var existing = await _context.SqlTests
                .FirstOrDefaultAsync(t => t.Id == id && t.CreatedByUserId == userId);

            if (existing == null)
                return NotFound();

            existing.Name = updatedTest.Name;
            existing.SqlQuery = updatedTest.SqlQuery;
            existing.ExpectedResult = updatedTest.ExpectedResult;
            existing.DatabaseConnectionName = updatedTest.DatabaseConnectionName;

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
    }
}

