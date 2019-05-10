using System;
using System.Collections.Generic;
using Flow.Rules.Engine.Interfaces;
using Flow.Rules.Engine.Models;
using Flow.Rules.UnitTests;
using Moq;
using Xunit;

namespace Flow.Rules.Engine.UnitTests
{
    public class PolicyExecutorTests
    {
        [Fact]
        public void ShouldExecuteAllRules()
        {
            Rule<PersonDataModel> rule1 = new Rule<PersonDataModel>
            {
                Id = Guid.NewGuid().ToString(),
                Name = "test rule 1",
                Description = "test description 1",
                Source = (request, lookups, calendar) => true
            };

            Rule<PersonDataModel> rule2 = new Rule<PersonDataModel>
            {
                Id = Guid.NewGuid().ToString(),
                Name = "test rule 2",
                Description = "test description 2",
                Source = (request, lookups, calendar) => true
            };

            Policy<PersonDataModel> policy = new Policy<PersonDataModel>("Test Policy", Guid.NewGuid().ToString(), new[] { rule1, rule2 });

            PersonDataModel personDataModel = new PersonDataModel
            {
                ShouldPass = true
            };

            Mock<ICalendarProvider> mockCalendarProvider = new Mock<ICalendarProvider>();
            mockCalendarProvider.Setup(m => m.CurrentDateTime).Returns(new DateTime(2017, 01, 02));

            PolicyExecutor policyExecutor = new PolicyExecutor(mockCalendarProvider.Object);

            IList<RuleExecutionResult> response = policyExecutor.Execute(policy, personDataModel, new Lookups());

            Assert.NotNull(response);
            Assert.Equal(2, response.Count);
        }
    }
}
