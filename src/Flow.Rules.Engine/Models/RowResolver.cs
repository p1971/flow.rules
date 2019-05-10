using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Flow.Rules.Engine.Models
{
    public class RowResolver
    {
        private readonly IDictionary<string, ColumnResolver> _dictionary
            = new ConcurrentDictionary<string, ColumnResolver>();

        public ColumnResolver this[string name]
        {
            get
            {
                if (!_dictionary.ContainsKey(name))
                {
                    _dictionary.Add(name, new ColumnResolver());
                }
                return _dictionary[name];
            }
            set
            {
                if (_dictionary.ContainsKey(name))
                {
                    _dictionary[name] = value;
                }
                _dictionary.Add(name, new ColumnResolver());
            }
        }
    }
}
