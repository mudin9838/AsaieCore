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
        var result = await _sovereignService.QuerySovereignLlamaDetailedAsync(
            dto.MemberState,
            dto.Prompt,
            dto.TargetLanguage ?? "en"
        );
        return Ok(result);
    }

    [HttpPost("query-stream")]
    public async Task StreamQuerySovereignNode(
        [FromBody] QueryDto dto,
        CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");

        await foreach (var token in _sovereignService.StreamSovereignLlamaAsync(
            dto.MemberState,
            dto.Prompt,
            dto.TargetLanguage ?? "en",
            cancellationToken))
        {
            await Response.WriteAsync(token, cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }
}

public record CreateRecordDto(string MemberState, string Region, string Hazard, string Content);

// Updated QueryDto to accept the multilingual language selection from the React UI
public record QueryDto(string MemberState, string Prompt, string? TargetLanguage = "en");