using System;
using System.Threading;
using System.Threading.Tasks;

namespace FlowRules.Engine.Models;

/// <summary>
/// Represents a rule that will be executed as part of the policy.
/// </summary>
/// <typeparam name="T">The type to execute against.</typeparam>
public class Rule<T>(string id, string name, string? description, Func<T, string>? failureMessage, Func<T, CancellationToken, ValueTask<bool>> source)
    where T : class
{
    /// <summary>
    /// Gets the id of the rule.
    /// </summary>
    public string Id { get; } = ValidateId(id);

    /// <summary>
    /// Gets the name of the rule.
    /// </summary>
    public string Name { get; } = ValidateName(name);

    /// <summary>
    /// Gets the description of the rule.
    /// </summary>
    public string? Description { get; } = description;

    /// <summary>
    /// Gets the function that returns a failure message for the rule.
    /// </summary>
    public Func<T, string>? FailureMessage { get; } = failureMessage;

    /// <summary>
    /// Gets the source of the rule.
    /// </summary>
    public Func<T, CancellationToken, ValueTask<bool>> Source { get; } = ValidateSource(source);

    private static string ValidateId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(id));
        return value;
    }

    private static string ValidateName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(name));
        return value;
    }

    private static Func<T, CancellationToken, ValueTask<bool>> ValidateSource(Func<T, CancellationToken, ValueTask<bool>> value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(source));
        return value;
    }
}
