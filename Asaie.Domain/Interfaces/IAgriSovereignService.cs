using Asaie.Domain.DTOs;
using Asaie.Domain.Entities;
namespace Asaie.Domain.Interfaces;

public interface IAgriSovereignService
{
    Task<AgriRiskRecord> AddRecordAsync(string memberState, string region, string hazard, string content);
    Task<IEnumerable<AgriRiskRecord>> SearchSimilarRecordsAsync(string memberState, string query, int topK = 3);
    Task<string> QuerySovereignLlamaAsync(string memberState, string prompt);
    Task<SovereignQueryResult> QuerySovereignLlamaDetailedAsync(string memberState, string prompt, string targetLanguage = "en");

}

