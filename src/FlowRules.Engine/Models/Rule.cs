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

    private static string ValidateId(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id, nameof(id));
        return id;
    }

    private static string ValidateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        return name;
    }

    private static Func<T, CancellationToken, ValueTask<bool>> ValidateSource(Func<T, CancellationToken, ValueTask<bool>> source)
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));
        return source;
    }
}
