using Flow.Rules.Engine.Interfaces;
using Flow.Rules.Engine.Models;

namespace Flow.Rules.Engine
{
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
}
