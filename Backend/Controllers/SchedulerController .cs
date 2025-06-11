using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SchedulerController : ControllerBase
    {
        private readonly SchedulerService _schedulerService;

        public SchedulerController(SchedulerService schedulerService)
        {
            _schedulerService = schedulerService;
        }

        [HttpPost("schedule")]
        public async Task<IActionResult> ScheduleScenario([FromBody] ScenarioScheduleDto schedule)
        {
            await _schedulerService.ScheduleScenarioAsync(schedule);
            return Ok("Scenario scheduled");
        }
        [HttpDelete("cancel")]
        public async Task<IActionResult> CancelScenario([FromQuery] string scenarioId, [FromQuery] string userId)
        {
            var result = await _schedulerService.CancelScheduledScenarioAsync(scenarioId, userId);
            if (result)
                return Ok("Scenario schedule cancelled.");
            else
                return NotFound("Scheduled job not found.");
        }
    }
}
