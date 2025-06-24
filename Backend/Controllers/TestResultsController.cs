using Backend.AppDbContext;

using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestResultsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TestResultsController(ApplicationDbContext context)
        {
            _context = context;
        }

       
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TestResultDto>>> GetTestResults(string? userId = null)
        {
            var query = _context.TestResults.AsQueryable();

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(tr => tr.ExecutedByUserId == userId);
            }

            var results = await query
                .Select(tr => new TestResultDto
                {
                    ExecutedAt = tr.ExecutedAt,
                    IsSuccess = tr.IsSuccess,
                    Response = tr.Response,
                    ErrorMessage = tr.ErrorMessage,
                    ApiTestId = tr.ApiTestId,
                    DurationMilliseconds = tr.DurationMilliseconds,
                    SqlTestId = tr.SqlTestId,
                    ExecutedByUserId = tr.ExecutedByUserId
                })
                .ToListAsync();

            return Ok(results);
        }
    }
}