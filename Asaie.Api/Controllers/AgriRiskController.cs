using Asaie.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Asaie.Api.Controllers;

[ApiController]
[Route("api/v1/sovereign/[controller]")]
public class AgriRiskController : ControllerBase
{
    private readonly IAgriSovereignService _sovereignService;

    public AgriRiskController(IAgriSovereignService sovereignService)
    {
        _sovereignService = sovereignService;
    }

    [HttpPost("record")]
    public async Task<IActionResult> CreateRecord([FromBody] CreateRecordDto dto)
    {
        var result = await _sovereignService.AddRecordAsync(dto.MemberState, dto.Region, dto.Hazard, dto.Content);
        return Ok(result);
    }

    [HttpPost("query")]
    public async Task<IActionResult> QuerySovereignNode([FromBody] QueryDto dto)
    {
        // 1. Pass TargetLanguage (defaults to "en" if null)
        // 2. Call the detailed service method that returns metrics for the audit trail
        var result = await _sovereignService.QuerySovereignLlamaDetailedAsync(
            dto.MemberState,
            dto.Prompt,
            dto.TargetLanguage ?? "en"
        );

        // 3. Return the full structured object so React can render the response + audit metrics
        return Ok(result);
    }
}

public record CreateRecordDto(string MemberState, string Region, string Hazard, string Content);

// Updated QueryDto to accept the multilingual language selection from the React UI
public record QueryDto(string MemberState, string Prompt, string? TargetLanguage = "en");