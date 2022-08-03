using System.Collections.Generic;

namespace FlowRules.Engine.Models
{
    public class Policy<T> where T : class
    {
        public Policy(string id, string name)
            : this(id, name, new List<Rule<T>>())
        {
        }

        public Policy(string id, string name, IList<Rule<T>> rules)
        {
            Id = id;
            Name = name;
            Rules = rules;
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public IList<Rule<T>> Rules { get; set; }
    }
}
