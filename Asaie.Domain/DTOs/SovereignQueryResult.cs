namespace Asaie.Domain.DTOs;

public record SovereignQueryResult(
    string MemberState,
    string Response,
    List<ContextRecordDto> RetrievedContext,
    double ExecutionTimeMs
);