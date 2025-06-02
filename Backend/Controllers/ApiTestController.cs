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
    public class ApiTestsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ApiTestsController(ApplicationDbContext context)
        {
            _context = context;
        }

        
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ApiTest>>> GetUserApiTests()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return await _context.ApiTests
                .Where(t => t.CreatedByUserId == userId)
                .ToListAsync();
        }

     
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiTest>> GetApiTest(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var test = await _context.ApiTests
                .FirstOrDefaultAsync(t => t.Id == id && t.CreatedByUserId == userId);

            if (test == null)
                return NotFound();

            return test;
        }

        
        [HttpPost]
        public async Task<ActionResult<ApiTest>> CreateApiTest(ApiTest apiTest)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            apiTest.CreatedByUserId = userId;

            _context.ApiTests.Add(apiTest);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetApiTest), new { id = apiTest.Id }, apiTest);
        }

        
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateApiTest(int id, ApiTest updatedTest)
        {
            if (id != updatedTest.Id)
                return BadRequest();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var existing = await _context.ApiTests
                .FirstOrDefaultAsync(t => t.Id == id && t.CreatedByUserId == userId);

            if (existing == null)
                return NotFound();

            existing.Name = updatedTest.Name;
            existing.Url = updatedTest.Url;
            existing.Method = updatedTest.Method;
            existing.Body = updatedTest.Body;
            existing.ExpectedResponse = updatedTest.ExpectedResponse;
            existing.ExpectedStatusCode = updatedTest.ExpectedStatusCode;

            await _context.SaveChangesAsync();
            return NoContent();
        }

      
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteApiTest(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var existing = await _context.ApiTests
                .FirstOrDefaultAsync(t => t.Id == id && t.CreatedByUserId == userId);

            if (existing == null)
                return NotFound();

            _context.ApiTests.Remove(existing);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
