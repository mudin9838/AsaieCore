using System.ComponentModel.DataAnnotations.Schema;
using Vector = Pgvector.Vector; // <-- Prevents System.Numerics collision

namespace Asaie.Domain.Entities;

public class AgriRiskRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Sovereign Multi-Tenancy Key (AU Member State code e.g., "ET", "KE", "NG")
    public string MemberStateCode { get; set; } = string.Empty;

    public string RegionName { get; set; } = string.Empty;
    public string HazardType { get; set; } = string.Empty; // Drought, Pest Locust, Flood
    public string Content { get; set; } = string.Empty;

    // Vector Embedding for Semantic Search / Local RAG (384 dimensions for all-minilm)
    [Column(TypeName = "vector(384)")]
    public Vector? Embedding { get; set; } // Now strictly Pgvector.Vector

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}