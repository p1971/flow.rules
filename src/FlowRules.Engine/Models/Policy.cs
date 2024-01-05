using System.Collections.Generic;

namespace FlowRules.Engine.Models;

/// <summary>
/// Represents a rules policy.
/// </summary>
/// <typeparam name="T">The type the policy is to be executed against.</typeparam>
public class Policy<T>
    where T : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Policy{T}"/> class.
    /// </summary>
    /// <param name="id">The id of the policy.</param>
    /// <param name="name">The name of the policy.</param>
    /// <param name="description">The description of the policy.</param>
    /// <param name="rules">The rules for the policy.</param>
    public Policy(string id, string name, string description, IList<Rule<T>> rules)
    {
        Id = id;
        Name = name;
        Description = description;
        Rules = rules;
    }

    /// <summary>
    /// Gets the id of the policy.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the name of the policy.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the description of the policy.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the rules of the policy.
    /// </summary>
    public IList<Rule<T>> Rules { get; }
}
