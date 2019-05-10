namespace Flow.Rules.Engine.Models
{
    public class FlowRulesOptions
    {
        public FlowRulesOptions()
        {
            Lookups = new Lookups();
        }

        public Lookups Lookups { get; set; }
    }
}
