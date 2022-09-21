using FlowRules.Samples.TestPolicy.Interfaces;

namespace FlowRules.Samples.TestPolicy;

public class LookupProvider : ILookupProvider
{
    private readonly Lookups _lookups;

    public LookupProvider(Lookups lookups)
    {
        _lookups = lookups;
    }

    public Lookups GetLookups()
    {
        return _lookups;
    }
}
