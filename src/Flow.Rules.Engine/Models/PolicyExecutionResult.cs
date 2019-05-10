using System;

namespace Flow.Rules.Engine.Models
{
    public class PolicyExecutionResult
    {
        public string PolicyName { get; set; }

        public string PolicyId { get; set; }

        public bool Passed { get; set; }

        public string Message { get; set; }

        public RuleExecutionResult[] RuleExecutionResults { get; set; }
        public Guid RuleContextId { get; set; }
    }
}
