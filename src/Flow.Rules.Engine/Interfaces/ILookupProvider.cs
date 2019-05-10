using Flow.Rules.Engine.Models;

namespace Flow.Rules.Engine.Interfaces
{
    public interface ILookupProvider
    {
        Lookups GetLookups();
    }
}