using FlowRules.Engine.Models;

namespace FlowRules.Samples.WebApi;

/// <summary>
/// JSON-safe API contract for a policy execution.
/// </summary>
internal sealed record PolicyExecutionResponse(
    string? PolicyName,
    string? PolicyId,
    bool Passed,
    RuleExecutionResponse[] RuleExecutionResults,
    Guid RuleContextId,
    string? CorrelationId,
    string? Version)
{
    public static PolicyExecutionResponse From(PolicyExecutionResult result) =>
        new(
            result.PolicyName,
            result.PolicyId,
            result.Passed,
            result.RuleExecutionResults.Select(RuleExecutionResponse.From).ToArray(),
            result.RuleContextId,
            result.CorrelationId,
            result.Version);
}

/// <summary>
/// Exposes the handled error as text instead of attempting to serialize the
/// runtime <see cref="Exception"/> object held by the engine result.
/// </summary>
internal sealed record RuleExecutionResponse(
    string Id,
    string Name,
    bool Passed,
    TimeSpan Elapsed,
    string? Description,
    string? Message,
    string? ExceptionType)
{
    public static RuleExecutionResponse From(RuleExecutionResult result) =>
        new(
            result.Id,
            result.Name,
            result.Passed,
            result.Elapsed,
            result.Description,
            result.Message,
            result.Exception?.GetType().Name);
}
