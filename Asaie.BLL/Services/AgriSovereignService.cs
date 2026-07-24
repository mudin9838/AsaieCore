using Asaie.DAL.Context;
using Asaie.Domain.Entities;
using Asaie.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using OllamaSharp;
using OllamaSharp.Models;
using Pgvector.EntityFrameworkCore;
using System.Text;
using Vector = Pgvector.Vector; // Resolves SIMD System.Numerics type collision

namespace Asaie.BLL.Services;

public class AgriSovereignService : IAgriSovereignService
{
    private readonly AsaieDbContext _context;
    private readonly IOllamaApiClient _ollamaClient;

    public AgriSovereignService(AsaieDbContext context, IOllamaApiClient ollamaClient)
    {
        _context = context;
        _ollamaClient = ollamaClient;
    }

    /// <summary>
    /// Ingests a new agricultural risk record, generates local vector embeddings via Ollama,
    /// and persists it to PostgreSQL tagged with a specific sovereign AU Member State code.
    /// </summary>
    public async Task<AgriRiskRecord> AddRecordAsync(string memberState, string region, string hazard, string content)
    {
        // 1. Generate local vector embedding using all-minilm via OllamaSharp EmbedRequest
        var embedRequest = new EmbedRequest
        {
            Model = "all-minilm",
            Input = new List<string> { content }
        };

        var embedResponse = await _ollamaClient.EmbedAsync(embedRequest);

        // Extract 384-dimension float vector from response
        float[] floatArray = embedResponse.Embeddings[0];

        // 2. Map to Sovereign Entity
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

    /// <summary>
    /// Performs a sovereign vector cosine similarity search strictly isolated 
    /// to the specified AU Member State's data partition.
    /// </summary>
    public async Task<IEnumerable<AgriRiskRecord>> SearchSimilarRecordsAsync(string memberState, string query, int topK = 3)
    {
        // 1. Embed incoming query vector
        var embedRequest = new EmbedRequest
        {
            Model = "all-minilm",
            Input = new List<string> { query }
        };
        var embedResponse = await _ollamaClient.EmbedAsync(embedRequest);
        var queryVector = new Vector(embedResponse.Embeddings[0]);

        // 2. Multi-tenant Sovereign Filtering & Vector Distance Calculation
        return await _context.AgriRiskRecords
            .Where(x => x.MemberStateCode == memberState.ToUpperInvariant())
            .OrderBy(x => x.Embedding!.CosineDistance(queryVector))
            .Take(topK)
            .ToListAsync();
    }

    /// <summary>
    /// Executes a Sovereign Retrieval-Augmented Generation (RAG) pipeline:
    /// Fetches local vector context and streams Llama 3 generation without sending data off-node.
    /// </summary>
    public async Task<string> QuerySovereignLlamaAsync(string memberState, string prompt)
    {
        // 1. Fetch relevant sovereign context from vector store
        var contextRecords = await SearchSimilarRecordsAsync(memberState, prompt, topK: 3);

        var contextText = string.Join("\n", contextRecords.Select(r => $"[{r.HazardType} in {r.RegionName}]: {r.Content}"));

        // 2. Construct sovereign system prompt template
        var fullPrompt = $"""
        You are ASAIE, a sovereign AI assistant for AU Member State: {memberState.ToUpperInvariant()}.
        Use ONLY the following sovereign local context to answer the prompt. If the context is insufficient, state it clearly.
        
        [SOVEREIGN CONTEXT DATA]
        {contextText}
        
        [USER QUERY]
        {prompt}
        """;

        // 3. Stream and assemble token response from local Ollama node
        var responseBuilder = new StringBuilder();

        await foreach (var responseStream in _ollamaClient.GenerateAsync(fullPrompt))
        {
            if (responseStream != null && !string.IsNullOrEmpty(responseStream.Response))
            {
                responseBuilder.Append(responseStream.Response);
            }
        }

        return responseBuilder.ToString();
    }
}