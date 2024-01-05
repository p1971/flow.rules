namespace FlowRules.Samples.TestPolicy;

public class ValueResolver
{
    private readonly object? value;

    public ValueResolver(object? value)
    {
        this.value = value;
    }

    public object? AsObject
    {
        get
        {
            return value;
        }
    }

    public string? AsString
    {
        get
        {
            return (string)value!;
        }
    }

    public T As<T>() where T : struct
    {
        if (value == null)
        {
            return default;
        }

        return typeof(T) switch
        {
            var testInt when testInt.GetType() == typeof(int) => (T)Convert.ChangeType(testInt, typeof(T)),
            _ => default
        };
    }
}
