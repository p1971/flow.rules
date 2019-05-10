using System;
using System.Collections.Generic;
using Flow.Rules.Engine.Interfaces;
using Flow.Rules.Engine.Models;
using Flow.Rules.UnitTests;
using Moq;
using Xunit;

namespace Flow.Rules.Engine.UnitTests
{
    public class PolicyManagerTests
    {
        [Fact]
        public void PolicyManager_Execute_Should_CallForKnownPolicy()
        {
            const string policyName = "policy1";

            Mock<IPolicyExecutor> mockPolicyExecutor =
                new Mock<IPolicyExecutor>();

            Mock<ILookupProvider> mockLookupProvider =
                new Mock<ILookupProvider>();

            var policy = new Policy<PersonDataModel>("p1", policyName);

            IPolicyManager<PersonDataModel> policyManager =
                new PolicyManager<PersonDataModel>(policy, mockPolicyExecutor.Object, mockLookupProvider.Object);

            mockPolicyExecutor.Setup(a => a.Execute(policy, It.IsAny<PersonDataModel>(), It.IsAny<Lookups>()))
                .Returns(new List<RuleExecutionResult>());

            PolicyExecutionResult response = policyManager.Execute(Guid.NewGuid(), policyName, new PersonDataModel());

            Assert.True(response.Passed);
            Assert.Equal(policyName, response.PolicyName);
        }
    }
}
