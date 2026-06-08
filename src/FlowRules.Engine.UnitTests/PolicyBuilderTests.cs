using System;
using System.Collections.Generic;
using System.Threading;
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
            .WithVersion("1.2.3")
            .WithRule("R01", "Rule", (model, token) => Task.FromResult(true), description: "rule desc", failureMessage: model => "test message")
            .Build();

        Assert.Equal("T001", policy.Id);
        Assert.Equal("Name", policy.Name);
        Assert.Equal("Desc", policy.Description);
        Assert.Equal("1.2.3", policy.Version);
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

    [Fact]
    public void Build_Should_Create_Rule_Snapshot()
    {
        PolicyBuilder<PersonDataModel> builder = PolicyBuilder<PersonDataModel>.Create()
            .WithId("T001")
            .WithName("Name")
            .WithRule("R01", "Rule", (model, token) => Task.FromResult(true));

        Policy<PersonDataModel> policy = builder.Build();

        builder.WithRule("R02", "Second Rule", (model, token) => Task.FromResult(true));

        Assert.Single(policy.Rules);
        Assert.Equal("R01", policy.Rules[0].Id);
    }

    [Fact]
    public void Policy_Should_Create_Rule_Snapshot()
    {
        List<Rule<PersonDataModel>> rules =
        [
            new("R01", "Rule", null, null, (model, token) => ValueTask.FromResult(true))
        ];

        Policy<PersonDataModel> policy = new("T001", "Name", "Desc", rules);

        rules.Add(new Rule<PersonDataModel>("R02", "Second Rule", null, null, (model, token) => ValueTask.FromResult(true)));

        Assert.Single(policy.Rules);
        Assert.Equal("R01", policy.Rules[0].Id);
    }

    [Fact]
    public void Build_Should_Throw_When_Duplicate_RuleIds_Are_Set()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            _subject
                .WithId("T001")
                .WithName("Name")
                .WithRule("R01", "Rule", (model, token) => Task.FromResult(true))
                .WithRule("R01", "Duplicate Rule", (model, token) => Task.FromResult(true))
                .Build());

        Assert.Contains("duplicate rule ids", ex.Message);
        Assert.Contains("R01", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Rule_Should_Throw_When_Id_Is_Invalid(string? id)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new Rule<PersonDataModel>(id!, "Rule", null, null, (model, token) => ValueTask.FromResult(true)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Rule_Should_Throw_When_Name_Is_Invalid(string? name)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new Rule<PersonDataModel>("R01", name!, null, null, (model, token) => ValueTask.FromResult(true)));
    }

    [Fact]
    public void Rule_Should_Throw_When_Source_IsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Rule<PersonDataModel>("R01", "Rule", null, null, null!));
    }

    [Fact]
    public void WithRule_Should_Throw_When_Task_Source_IsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _subject.WithRule("R01", "Rule", (Func<PersonDataModel, CancellationToken, Task<bool>>)null!));
    }
}
