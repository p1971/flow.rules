namespace Flow.Rules.Engine.Models
{
    public class RuleExecutionResult
    {
        public RuleExecutionResult(string id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
        }

        public string Id { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public bool Passed { get; set; }

        public string Message { get; set; }
    }
}
