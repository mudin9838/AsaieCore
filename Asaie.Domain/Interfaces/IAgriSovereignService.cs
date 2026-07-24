using Asaie.Domain.Entities;

namespace Asaie.Domain.Interfaces;

public interface IAgriSovereignService
{
    Task<AgriRiskRecord> AddRecordAsync(string memberState, string region, string hazard, string content);
    Task<IEnumerable<AgriRiskRecord>> SearchSimilarRecordsAsync(string memberState, string query, int topK = 3);
    Task<string> QuerySovereignLlamaAsync(string memberState, string prompt);
}