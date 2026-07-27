namespace Asaie.Domain.DTOs;

public record ContextRecordDto(
    string Region,
    string Hazard,
    string Content,
    double CosineDistance
);