﻿using System.Collections.Concurrent;

namespace FlowRules.Samples.TestPolicy;

public class Lookups
{
    private readonly IDictionary<string, RowResolver> _dictionary
        = new ConcurrentDictionary<string, RowResolver>();

    public Lookups()
        : this(new List<(string page, string row, string column, object value)>())
    {
    }

    public Lookups(List<(string page, string row, string column, object value)> data)
    {
        foreach ((string page, string row, string column, object value) in data)
        {
            this[page][row][column] = new ValueResolver(value);
        }
    }

    public RowResolver this[string name]
    {
        get
        {
            if (!_dictionary.ContainsKey(name))
            {
                _dictionary.Add(name, new RowResolver());
            }

            return _dictionary[name];
        }
        set
        {
            if (_dictionary.ContainsKey(name))
            {
                _dictionary[name] = value;
            }
            _dictionary.Add(name, new RowResolver());
        }
    }
}
