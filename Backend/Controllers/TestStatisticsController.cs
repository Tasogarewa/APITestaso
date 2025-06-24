using Backend.AppDbContext;
using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TestStatisticsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly TestStatisticsService _statisticsService;

        public TestStatisticsController(ApplicationDbContext context, TestStatisticsService statisticsService)
        {
            _context = context;
            _statisticsService = statisticsService;
        }

        [HttpGet("general")]
        public async Task<ActionResult<TestStatsDto>> GetGeneralStatistics()
        {
            var testResults = await _context.TestResults
                .Include(r => r.ApiTest)
                .Include(r => r.SqlTest)
                .ToListAsync();

            var stats = _statisticsService.ComputeStatistics(testResults);
            return Ok(stats);
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<TestStatsDto>> GetStatisticsByUser(string userId)
        {
            var testResults = await _context.TestResults
                .Include(r => r.ApiTest)
                .Include(r => r.SqlTest)
                .Where(r => r.ExecutedByUserId == userId)
                .ToListAsync();

            var stats = _statisticsService.ComputeStatistics(testResults);
            return Ok(stats);
        }
    }

}
