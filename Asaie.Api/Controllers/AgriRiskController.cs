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
        var answer = await _sovereignService.QuerySovereignLlamaAsync(dto.MemberState, dto.Prompt);
        return Ok(new { memberState = dto.MemberState, response = answer });
    }
}

public record CreateRecordDto(string MemberState, string Region, string Hazard, string Content);
public record QueryDto(string MemberState, string Prompt);
