using System;
using Flow.Rules.Engine.Interfaces;

namespace Flow.Rules.Engine.Models
{
    public class Rule<T> where T : class
    {
        public Rule()
        {
        }

        public Rule(string id, string name, string description, Func<T, string> failureMessage, Func<T, Lookups, ICalendarProvider, bool> source)
        {
            Id = id;
            Name = name;
            Description = description;
            FailureMessage = failureMessage;
            Source = source;
        }

        public string Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public Func<T, string> FailureMessage { get; set; }

        public Func<T, Lookups, ICalendarProvider, bool> Source { get; set; }
    }
}
