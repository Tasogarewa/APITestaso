using Backend.AppDbContext;
using Backend.DTOs;
using Backend.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ApiTestsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IValidator<ApiTestDto> _validator;

    public ApiTestsController(ApplicationDbContext context, IValidator<ApiTestDto> validator)
    {
        _context = context;
        _validator = validator;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApiTestDto>>> GetUserApiTests()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var tests = await _context.ApiTests
            .Where(t => t.CreatedByUserId == userId)
            .ToListAsync();

        var dtos = tests.Select(t => MapToDto(t)).ToList();
        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiTestDto>> GetApiTest(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var test = await _context.ApiTests
            .FirstOrDefaultAsync(t => t.Id == id && t.CreatedByUserId == userId);

        if (test == null)
            return NotFound();

        return Ok(MapToDto(test));
    }

    [HttpPost]
    public async Task<ActionResult<ApiTestDto>> CreateApiTest([FromBody] ApiTestDto apiTestDto)
    {
        var validationResult = await _validator.ValidateAsync(apiTestDto);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var apiTest = MapToModel(apiTestDto);
        apiTest.CreatedByUserId = userId;
        apiTest.CreatedByUser = null!;
        _context.ApiTests.Add(apiTest);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetApiTest), new { id = apiTest.Id }, MapToDto(apiTest));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateApiTest(int id, ApiTestDto updatedDto)
    {
        if (id != updatedDto.Id)
            return BadRequest();

        var validationResult = await _validator.ValidateAsync(updatedDto);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var existing = await _context.ApiTests
            .FirstOrDefaultAsync(t => t.Id == id && t.CreatedByUserId == userId);

        if (existing == null)
            return NotFound();

        
        existing.Name = updatedDto.Name;
        existing.Url = updatedDto.Url;
        existing.Method = updatedDto.Method;
        existing.BodyJson = updatedDto.BodyJson;
        existing.ExpectedResponse = updatedDto.ExpectedResponse;
        existing.QueryParameters = updatedDto.QueryParameters ?? new Dictionary<string, string>();
        existing.ExpectedStatusCode = updatedDto.ExpectedStatusCode;
        existing.IsMock = updatedDto.IsMock;
        existing.TimeoutSeconds = updatedDto.TimeoutSeconds;
        existing.Headers = updatedDto.Headers;
        existing.Save = updatedDto.Save;

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

    private ApiTestDto MapToDto(ApiTest test) =>
        new ApiTestDto
        {
            Id = test.Id,
            Name = test.Name,
            Url = test.Url,
            Method = test.Method,
            BodyJson = test.BodyJson,
            QueryParameters = test.QueryParameters,
            ExpectedResponse = test.ExpectedResponse,
            ExpectedStatusCode = test.ExpectedStatusCode,
            IsMock = test.IsMock,
            TimeoutSeconds = test.TimeoutSeconds,
            Headers = test.Headers,
            Save = test.Save,
            CreatedByUserId = test.CreatedByUserId
        };

    private ApiTest MapToModel(ApiTestDto dto) =>
        new ApiTest
        {
            
            Id = dto.Id,
            Name = dto.Name,
            Url = dto.Url,
            Method = dto.Method,
            BodyJson = dto.BodyJson,
             QueryParameters = dto.QueryParameters,
            ExpectedResponse = dto.ExpectedResponse,
            ExpectedStatusCode = dto.ExpectedStatusCode,
            IsMock = dto.IsMock,
            TimeoutSeconds = dto.TimeoutSeconds,
            Headers = dto.Headers,
            Save = dto.Save,
            CreatedByUserId = dto.CreatedByUserId
        };
}