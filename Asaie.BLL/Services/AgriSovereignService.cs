using Asaie.DAL.Context;
using Asaie.Domain.DTOs;
using Asaie.Domain.Entities;
using Asaie.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using OllamaSharp;
using OllamaSharp.Models;
using Pgvector.EntityFrameworkCore;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Vector = Pgvector.Vector; // Resolves SIMD System.Numerics type collision

namespace Asaie.BLL.Services;

public class AgriSovereignService : IAgriSovereignService
{
    private readonly AsaieDbContext _context;
    private readonly IOllamaApiClient _ollamaClient;

    private const string EMBEDDING_MODEL = "all-minilm";
    private const string GENERATION_MODEL = "qwen2.5:7b";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AgriSovereignService(AsaieDbContext context, IOllamaApiClient ollamaClient)
    {
        _context = context;
        _ollamaClient = ollamaClient;
    }

    public async Task<AgriRiskRecord> AddRecordAsync(string memberState, string region, string hazard, string content)
    {
        var embedRequest = new EmbedRequest
        {
            Model = EMBEDDING_MODEL,
            Input = new List<string> { content }
        };

        var embedResponse = await _ollamaClient.EmbedAsync(embedRequest);
        float[] floatArray = embedResponse.Embeddings[0];

        var record = new AgriRiskRecord
        {
            MemberStateCode = memberState.ToUpperInvariant(),
            RegionName = region,
            HazardType = hazard,
            Content = content,
            Embedding = new Vector(floatArray)
        };

        _context.AgriRiskRecords.Add(record);
        await _context.SaveChangesAsync();

        return record;
    }

    public async Task<IEnumerable<AgriRiskRecord>> SearchSimilarRecordsAsync(string memberState, string query, int topK = 3)
    {
        var embedRequest = new EmbedRequest
        {
            Model = EMBEDDING_MODEL,
            Input = new List<string> { query }
        };
        var embedResponse = await _ollamaClient.EmbedAsync(embedRequest);
        var queryVector = new Vector(embedResponse.Embeddings[0]);

        return await _context.AgriRiskRecords
            .Where(x => x.MemberStateCode == memberState.ToUpperInvariant())
            .OrderBy(x => x.Embedding!.CosineDistance(queryVector))
            .Take(topK)
            .ToListAsync();
    }

    public async Task<string> QuerySovereignLlamaAsync(string memberState, string prompt)
    {
        var contextRecords = await SearchSimilarRecordsAsync(memberState, prompt, topK: 3);
        var contextText = string.Join("\n", contextRecords.Select(r => $"[{r.HazardType} in {r.RegionName}]: {r.Content}"));
        var fullPrompt = BuildPrompt(memberState, "en", contextText, prompt);

        var responseBuilder = new StringBuilder();
        var generateRequest = new GenerateRequest { Model = GENERATION_MODEL, Prompt = fullPrompt };

        await foreach (var responseStream in _ollamaClient.GenerateAsync(generateRequest))
        {
            if (responseStream != null && !string.IsNullOrEmpty(responseStream.Response))
            {
                responseBuilder.Append(responseStream.Response);
            }
        }

        return responseBuilder.ToString();
    }

    public async Task<SovereignQueryResult> QuerySovereignLlamaDetailedAsync(string memberState, string prompt, string targetLanguage = "en")
    {
        var stopwatch = Stopwatch.StartNew();

        var embedRequest = new EmbedRequest { Model = EMBEDDING_MODEL, Input = new List<string> { prompt } };
        var embedResponse = await _ollamaClient.EmbedAsync(embedRequest);
        var queryVector = new Vector(embedResponse.Embeddings[0]);

        var contextRecords = await _context.AgriRiskRecords
            .Where(x => x.MemberStateCode == memberState.ToUpperInvariant())
            .Select(x => new
            {
                Record = x,
                Distance = x.Embedding!.CosineDistance(queryVector)
            })
            .OrderBy(x => x.Distance)
            .Take(3)
            .ToListAsync();

        var contextText = string.Join("\n", contextRecords.Select(r => $"[{r.Record.HazardType} in {r.Record.RegionName}]: {r.Record.Content}"));
        var fullPrompt = BuildPrompt(memberState, targetLanguage, contextText, prompt);

        var responseBuilder = new StringBuilder();
        var generateRequest = new GenerateRequest
        {
            Model = GENERATION_MODEL,
            Prompt = fullPrompt,
            Options = new RequestOptions { NumPredict = 120, Temperature = 0.1f }
        };

        await foreach (var responseStream in _ollamaClient.GenerateAsync(generateRequest))
        {
            if (responseStream != null && !string.IsNullOrEmpty(responseStream.Response))
            {
                responseBuilder.Append(responseStream.Response);
            }
        }

        stopwatch.Stop();

        return new SovereignQueryResult(
            MemberState: memberState.ToUpperInvariant(),
            Response: responseBuilder.ToString(),
            RetrievedContext: contextRecords.Select(c => new ContextRecordDto(
                c.Record.RegionName,
                c.Record.HazardType,
                c.Record.Content,
                Math.Round(c.Distance, 4)
            )).ToList(),
            ExecutionTimeMs: Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2)
        );
    }

    public async IAsyncEnumerable<string> StreamSovereignLlamaAsync(
        string memberState,
        string prompt,
        string targetLanguage = "en",
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        var embedRequest = new EmbedRequest { Model = EMBEDDING_MODEL, Input = new List<string> { prompt } };
        var embedResponse = await _ollamaClient.EmbedAsync(embedRequest);
        var queryVector = new Vector(embedResponse.Embeddings[0]);

        var contextRecords = await _context.AgriRiskRecords
            .Where(x => x.MemberStateCode == memberState.ToUpperInvariant())
            .Select(x => new
            {
                Record = x,
                Distance = x.Embedding!.CosineDistance(queryVector)
            })
            .OrderBy(x => x.Distance)
            .Take(3)
            .ToListAsync(cancellationToken);

        var contextText = string.Join("\n", contextRecords.Select(r => $"[{r.Record.HazardType} in {r.Record.RegionName}]: {r.Record.Content}"));
        var fullPrompt = BuildPrompt(memberState, targetLanguage, contextText, prompt);

        var generateRequest = new GenerateRequest
        {
            Model = GENERATION_MODEL,
            Prompt = fullPrompt,
            Options = new RequestOptions { NumPredict = 120, Temperature = 0.1f }
        };

        await foreach (var responseStream in _ollamaClient.GenerateAsync(generateRequest, cancellationToken))
        {
            if (responseStream != null && !string.IsNullOrEmpty(responseStream.Response))
            {
                yield return responseStream.Response;
            }
        }

        stopwatch.Stop();

        var auditData = new SovereignQueryResult(
            MemberState: memberState.ToUpperInvariant(),
            Response: string.Empty,
            RetrievedContext: contextRecords.Select(c => new ContextRecordDto(
                c.Record.RegionName,
                c.Record.HazardType,
                c.Record.Content,
                Math.Round(c.Distance, 4)
            )).ToList(),
            ExecutionTimeMs: Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2)
        );

        // Append metadata delimiter with camelCase serialized audit data
        yield return $"\n[METADATA]{JsonSerializer.Serialize(auditData, _jsonOptions)}";
    }

    private static string BuildPrompt(string memberState, string targetLanguage, string contextText, string prompt)
    {
        string languageInstruction = targetLanguage.ToLower() switch
        {
            "ar" => """
                    You MUST respond 100% in native, pure Modern Standard Arabic (العربية الفصحى).
                    CRITICAL RULES:
                    1. Do NOT output any Latin, English, or Cyrillic characters under any circumstances.
                    2. Translate every single term, concept, and metric strictly into standard Arabic.
                    3. Keep sentences clear, concise, and structured with bullet points.
                    """,
            "am" => """
                    You MUST respond completely in native Amharic script (አማርኛ). 
                    Do NOT use Latin letters, English words, or phonetic transliterations in parentheses.
                    """,
            "sw" => "Respond ONLY in Swahili (Kiswahili). Translate all context and insights accurately.",
            "ha" => "Respond ONLY in Hausa. Translate all context and insights accurately.",
            "fr" => "Respond ONLY in French (Français). Translate all context and insights accurately.",
            _ => "Respond in English."
        };

        return $"""
                You are ASAIE, an official sovereign AI assistant for AU Member State: {memberState.ToUpperInvariant()}.
                {languageInstruction}
                Use ONLY the following sovereign local context to answer the user query. If context is insufficient, state it clearly in the chosen language.

                [SOVEREIGN CONTEXT DATA]
                {contextText}

                [USER QUERY]
                {prompt}
                """;
    }
}