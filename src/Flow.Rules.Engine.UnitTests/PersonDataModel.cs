using System;

namespace Flow.Rules.UnitTests
{
    internal class PersonDataModel
    {
        public string Name { get; set; }

        public DateTime DateOfBirth { get; set; }

        public bool ShouldPass { get; set; }
    }
}
