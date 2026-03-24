using System;
using Microsoft.Extensions.Logging;

namespace FlowRules.Engine
{
    internal static partial class LoggerExtensions
    {
        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Executing [{policyId}]:[{policyName}] for [{executionContextId}]")]
        public static partial void LogPolicyStartMessage(this ILogger logger, string policyId, string policyName, Guid executionContextId);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Executing [{ruleId}] for [{executionContextId}]")]
        public static partial void LogRuleStartMessage(this ILogger logger, string ruleId, Guid executionContextId);

        [LoggerMessage(
            Level = LogLevel.Error,
            Message = "An exception occurred writing the results to the [{repositoryTypeName}] for [{ruleContextId}]")]
        public static partial void LogExceptionWritingToRepository(this ILogger logger, Exception ex, string repositoryTypeName, Guid ruleContextId);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "... executing [{policyId}]:[{policyName}] for [{executionContextId}]")]
        public static partial void LogExecutionPolicy(this ILogger logger, string policyId, string policyName, Guid executionContextId);

        [LoggerMessage(
            Level = LogLevel.Error,
            Message = "Execution was cancelled [{ruleId}]:[{ruleName}]")]
        public static partial void LogRuleCancelled(this ILogger logger, string ruleId, string ruleName);

        [LoggerMessage(
            Level = LogLevel.Error,
            Message = "An exception occurred executing [{ruleId}]:[{ruleName}]")]
        public static partial void LogExceptionForRule(this ILogger logger, Exception ex, string ruleId, string ruleName);
    }
}
