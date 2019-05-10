using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Flow.Rules.Engine.Models
{
    public class ColumnResolver
    {
        private readonly IDictionary<string, object> _dictionary =
            new ConcurrentDictionary<string, object>();

        public object this[string name]
        {
            get
            {
                if (!_dictionary.ContainsKey(name))
                {
                    _dictionary.Add(name, null);
                }
                return _dictionary[name];
            }
            set
            {
                if (_dictionary.ContainsKey(name))
                {
                    _dictionary[name] = value;
                }
                _dictionary.Add(name, value);
            }
        }
    }
}
