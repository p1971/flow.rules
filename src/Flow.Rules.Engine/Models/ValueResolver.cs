using System;

namespace Flow.Rules.Engine.Models
{
    public class ValueResolver
    {
        private readonly object value;

        public ValueResolver(object value)
        {
            this.value = value;
        }

        public object AsObject
        {
            get
            {
                return this.value;
            }
        }

        public string AsString
        {
            get
            {
                return this.value as string;
            }
        }

        public T As<T>() where T : struct
        {
            if (this.value == null)
            {
                return default;
            }

            switch (typeof(T))
            {
                case var testInt when (testInt.GetType() == typeof(int)):
                    return (T)Convert.ChangeType(testInt, typeof(T));
                
            }

            return default;
        }
    }
}
