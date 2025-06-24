using Backend.DTOs;
using Backend.DTOs.Validators;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backend.Controllers
{
    [Authorize]
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
            var validator = new ScenarioScheduleDtoValidator();
            var validationResult = await validator.ValidateAsync(schedule);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
            }

            try
            {
                await _schedulerService.ScheduleScenarioAsync(schedule);
                return Ok("Scenario scheduled successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
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

        [HttpGet("scheduled")]
        public async Task<ActionResult<List<ScenarioScheduleDto>>> GetScheduledScenarios()
        {
            var scheduled = await _schedulerService.GetAllScheduledScenariosAsync();
            return Ok(scheduled);
        }
    }
}