using System;
using System.Diagnostics;

using FlowRules.Engine.Models;

namespace FlowRules.Engine.Interfaces
{
    /// <summary>
    /// Provides telemetry services for monitoring and tracking the execution of policies and rules.
    /// </summary>
    public interface IFlowRulesTelemetryService
    {
        /// <summary>
        /// Tracks the execution of a policy, recording telemetry data such as execution time and context information.
        /// </summary>
        /// <typeparam name="T">The type of the object the policy is executed against.</typeparam>
        /// <param name="policy">The policy being executed.</param>
        /// <param name="contextId">The unique identifier for the execution context.</param>
        /// <param name="correlationId">The correlation identifier for tracking related operations.</param>
        /// <param name="elapsedTime">The time taken to execute the policy.</param>
        void PolicyExecution<T>(Policy<T> policy, Guid contextId, string correlationId, TimeSpan elapsedTime)
            where T : class;

        /// <summary>
        /// Tracks the execution of a specific rule within a policy, recording telemetry data such as execution time and identifiers.
        /// </summary>
        /// <typeparam name="T">The type the policy and rule are executed against.</typeparam>
        /// <param name="policy">The policy containing the rule being executed.</param>
        /// <param name="rule">The rule being executed.</param>
        /// <param name="contextId">The unique identifier for the execution context.</param>
        /// <param name="correlationId">The identifier used to correlate related operations.</param>
        /// <param name="elapsedTime">The time taken to execute the rule.</param>
        void RuleExecution<T>(Policy<T> policy, Rule<T> rule, Guid contextId, string correlationId, TimeSpan elapsedTime)
            where T : class;

        /// <summary>
        /// Starts a telemetry activity for tracking the execution of a policy.
        /// </summary>
        /// <typeparam name="T">The type the policy is to be executed against.</typeparam>
        /// <param name="policy">The policy for which the activity is being started.</param>
        /// <param name="contextId">The unique identifier for the execution context.</param>
        /// <param name="correlationId">The correlation identifier for tracking related operations.</param>
        /// <returns>
        /// An <see cref="Activity"/> instance representing the started telemetry activity,
        /// or <c>null</c> if the activity could not be started.
        /// </returns>
        Activity? StartActivity<T>(Policy<T> policy, Guid contextId, string correlationId)
            where T : class;

        /// <summary>
        /// Starts a telemetry activity for the execution of a specific rule within a policy.
        /// </summary>
        /// <typeparam name="T">The type of the object the rule operates on.</typeparam>
        /// <param name="rule">The rule for which the activity is being started.</param>
        /// <param name="contextId">The unique identifier for the execution context.</param>
        /// <param name="correlationId">The correlation identifier used to track related operations.</param>
        /// <returns>
        /// An <see cref="Activity"/> instance representing the telemetry activity, or <c>null</c> if the activity could not be started.
        /// </returns>
        Activity? StartActivity<T>(Rule<T> rule, Guid contextId, string correlationId)
            where T : class;

        /// <summary>
        /// Marks the specified activity as successful by setting its status to <see cref="ActivityStatusCode.Ok"/>.
        /// </summary>
        /// <param name="activity">
        /// The <see cref="Activity"/> instance to mark as successful. If <c>null</c>, no action is performed.
        /// </param>
        void SetSuccess(Activity? activity);

        /// <summary>
        /// Marks the specified activity as a failure and associates it with an error message.
        /// </summary>
        /// <param name="activity">
        /// The <see cref="Activity"/> instance representing the telemetry context.
        /// Can be <c>null</c> if no activity is available.
        /// </param>
        /// <param name="errorMessage">
        /// A string containing the error message that describes the failure.
        /// </param>
        void SetFailure(Activity? activity, string errorMessage);
    }
}
