using System;
using System.Threading;
using System.Threading.Tasks;

namespace FlowRules.Engine.Models
{
    /// <summary>
    /// Represents a rule that will be executed as part of the policy.
    /// </summary>
    /// <typeparam name="T">The type to execute against.</typeparam>
    public class Rule<T>
        where T : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Rule{T}"/> class.
        /// </summary>
        /// <param name="id">The id of the rule.</param>
        /// <param name="name">The name of the rule.</param>
        /// <param name="description">The description of the rule.</param>
        /// <param name="failureMessage">A function that creates a failure message based on the input request.</param>
        /// <param name="source">The rule source.</param>
        public Rule(string id, string name, string description, Func<T, string> failureMessage, Func<T, CancellationToken, Task<bool>> source)
        {
            Id = id;
            Name = name;
            Description = description;
            FailureMessage = failureMessage;
            Source = source;
        }

        /// <summary>
        /// Gets the id of the rule.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the name of the rule.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the description of the rule.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the function that returns a failure message for the rule.
        /// </summary>
        public Func<T, string> FailureMessage { get; }

        /// <summary>
        /// Gets the source of the rule.
        /// </summary>
        public Func<T, CancellationToken, Task<bool>> Source { get; }
    }
}
