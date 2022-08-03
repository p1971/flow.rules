using System;
using System.Threading;
using System.Threading.Tasks;

namespace FlowRules.Engine.Models
{
    public class Rule<T> where T : class
    {
        public Rule()
        {
        }

        public Rule(string id, string name, string description, Func<T, string> failureMessage, Func<T, CancellationToken, Task<bool>> source)
        {
            Id = id;
            Name = name;
            Description = description;
            FailureMessage = failureMessage;
            Source = source;
        }

        public string Id { get; init; }

        public string Name { get; init; }

        public string Description { get; init; }

        public Func<T, string> FailureMessage { get; set; }

        public Func<T, CancellationToken, Task<bool>> Source { get; init; }
    }
}
