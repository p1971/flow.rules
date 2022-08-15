using System;

namespace FlowRules.Engine.Models
{
    /// <summary>
    /// Represents the results of executing arule as part of a policy.
    /// </summary>
    public class RuleExecutionResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RuleExecutionResult"/> class.
        /// </summary>
        /// <param name="id">The id of the rule.</param>
        /// <param name="name">The name of the rule.</param>
        /// <param name="description">The description of the rule.</param>
        public RuleExecutionResult(string id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
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
        /// Gets or sets a value indicating whether the rule passed.
        /// </summary>
        public bool Passed { get; set; }

        /// <summary>
        /// Gets or sets the failure message for the rule.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the time taken to execute the rule.
        /// </summary>
        public TimeSpan Elapsed { get; set; }

        /// <summary>
        /// Gets or sets any exception associated with the rule.
        /// </summary>
        public Exception Exception { get; set; }
    }
}
