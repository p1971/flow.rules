using System;
using System.Threading.Tasks;

using FlowRules.Engine.Models;

using Xunit;

namespace FlowRules.Engine.UnitTests;

public class PolicyBuilderTests
{
    private readonly PolicyBuilder<PersonDataModel> _subject = new();

    [Fact]
    public void Build_Should_Throw_When_Id_Not_Set()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            _subject
                .WithName("Name")
                .Build());

        Assert.Contains("WithId", ex.Message);
    }

    [Fact]
    public void Build_Should_Throw_When_Name_Not_Set()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            _subject
                .WithId("T001")
                .Build());

        Assert.Contains("WithName", ex.Message);
    }

    [Fact]
    public void Build_Should_Throw_When_No_Rules_Are_Set()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            _subject
                .WithId("T001")
                .WithName("Name")
                .Build());

        Assert.Contains("WithRule", ex.Message);
    }

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

    [Fact]
    public void Policy_Should_Throw_When_Constructed_With_Empty_Rules()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new Policy<PersonDataModel>("T001", "Name", "Desc", []));

        Assert.Contains("at least one rule", ex.Message);
    }
}
