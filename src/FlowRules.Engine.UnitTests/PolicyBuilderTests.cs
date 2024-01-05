using System.Threading.Tasks;

using FlowRules.Engine.Models;

using Xunit;

namespace FlowRules.Engine.UnitTests;

public class PolicyBuilderTests
{
    private readonly PolicyBuilder<PersonDataModel> _subject = new();

    [Fact]
    public void Build_Should_Map_AllProperties()
    {
        Policy<PersonDataModel> policy = _subject
            .WithId("T001")
            .WithName("Name")
            .WithDescription("Desc")
            .WithRule("R01", "Rule", (model, token) => Task.FromResult(true), description: "rule desc", failureMessage: model => "test message")
            .Build();

        Assert.Equal("T001", policy.Id);
        Assert.Equal("Name", policy.Name);
        Assert.Equal("Desc", policy.Description);
        Assert.Single(policy.Rules);

        Rule<PersonDataModel> rule = policy.Rules[0];

        Assert.Equal("R01", rule.Id);
        Assert.Equal("Rule", rule.Name);
        Assert.Equal("rule desc", rule.Description);
        Assert.NotNull(rule.Source);
        Assert.NotNull(rule.FailureMessage);
    }
}
