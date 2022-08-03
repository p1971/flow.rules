using System;
using System.Threading;
using System.Threading.Tasks;

using FlowRules.Engine.Models;

using Xunit;

namespace FlowRules.Engine.UnitTests
{
    public class PolicyManagerTests
    {
        [Fact]
        public async Task ShouldExecuteAllRules()
        {
            Rule<PersonDataModel> rule1 = new()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "test rule 1",
                Description = "test description 1",
                Source = (request, token) => Task.FromResult(true)
            };

            Rule<PersonDataModel> rule2 = new()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "test rule 2",
                Description = "test description 2",
                Source = (request, token) => Task.FromResult(true)
            };

            Policy<PersonDataModel> policy = new("Test Policy", Guid.NewGuid().ToString(), new[]
            {
                rule1,
                rule2
            });

            PersonDataModel personDataModel = new()
            {
                ShouldPass = true
            };


            PolicyManager<PersonDataModel> policyManager = new(policy, new DefaultPolicyResultsRepository<PersonDataModel>());

            CancellationTokenSource cancellationTokenSource = new();
            CancellationToken cancellationToken = cancellationTokenSource.Token;


            PolicyExecutionResult response
                = await policyManager.Execute(Guid.NewGuid(), personDataModel, cancellationToken);

            Assert.NotNull(response);
            Assert.Equal(2, response.RuleExecutionResults.Length);
        }
    }
}
