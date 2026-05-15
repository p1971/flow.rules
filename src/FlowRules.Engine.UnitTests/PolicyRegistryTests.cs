using System;
using System.Threading;
using System.Threading.Tasks;

using FlowRules.Engine.Interfaces;
using FlowRules.Engine.Models;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Xunit;
using Xunit.Abstractions;

namespace FlowRules.Engine.UnitTests;

public class PolicyRegistryTests(ITestOutputHelper testOutputHelper)
{
    private readonly IFlowRulesTelemetryService _telemetryService = Substitute.For<IFlowRulesTelemetryService>();

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private PolicyManager<T> BuildManager<T>(Policy<T> policy)
        where T : class
    {
        ILogger<PolicyManager<T>> logger = testOutputHelper.BuildLoggerFor<PolicyManager<T>>();
        IPolicyResultsRepository<T> repo = Substitute.For<IPolicyResultsRepository<T>>();
        return new PolicyManager<T>(policy, repo, _telemetryService, logger);
    }

    private static Policy<PersonDataModel> MakePersonPolicy(string id, string name = "Person Policy") =>
        PolicyBuilder<PersonDataModel>.Create()
            .WithId(id)
            .WithName(name)
            .WithDescription("test")
            .WithRule("R001", "always pass", (_, _) => Task.FromResult(true))
            .Build();

    private static Policy<AddressDataModel> MakeAddressPolicy(string id, string name = "Address Policy") =>
        PolicyBuilder<AddressDataModel>.Create()
            .WithId(id)
            .WithName(name)
            .WithDescription("test")
            .WithRule("R001", "always pass", (_, _) => Task.FromResult(true))
            .Build();

    private static PolicyRegistry BuildRegistry(params IPolicyRegistryEntry[] entries) =>
        new(entries);

    // ---------------------------------------------------------------------------
    // PolicyIds
    // ---------------------------------------------------------------------------

    [Fact]
    public void PolicyIds_Returns_All_Registered_Ids()
    {
        Policy<PersonDataModel> p1 = MakePersonPolicy("P001");
        Policy<AddressDataModel> p2 = MakeAddressPolicy("P002");

        PolicyRegistry registry = BuildRegistry(
            new PolicyRegistryEntry<PersonDataModel>("P001", BuildManager(p1)),
            new PolicyRegistryEntry<AddressDataModel>("P002", BuildManager(p2)));

        Assert.Contains("P001", registry.PolicyIds);
        Assert.Contains("P002", registry.PolicyIds);
        Assert.Equal(2, registry.PolicyIds.Count);
    }

    // ---------------------------------------------------------------------------
    // ExecuteAsync — happy paths
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task ExecuteAsync_Executes_Correct_Policy_By_Id()
    {
        Policy<PersonDataModel> policy = MakePersonPolicy("P001");
        PolicyRegistry registry = BuildRegistry(
            new PolicyRegistryEntry<PersonDataModel>("P001", BuildManager(policy)));

        PersonDataModel request = new("Alice", new DateOnly(1990, 1, 1));
        PolicyExecutionResult result = await registry.ExecuteAsync<PersonDataModel>(
            "P001", "corr-1", Guid.NewGuid(), request, CancellationToken.None);

        Assert.Equal("P001", result.PolicyId);
        Assert.True(result.Passed);
    }

    [Fact]
    public async Task ExecuteAsync_Routes_To_Correct_Policy_When_Multiple_Registered()
    {
        Policy<PersonDataModel> personPolicy = MakePersonPolicy("PERSON-POLICY");
        Policy<AddressDataModel> addressPolicy = MakeAddressPolicy("ADDRESS-POLICY");

        PolicyRegistry registry = BuildRegistry(
            new PolicyRegistryEntry<PersonDataModel>("PERSON-POLICY", BuildManager(personPolicy)),
            new PolicyRegistryEntry<AddressDataModel>("ADDRESS-POLICY", BuildManager(addressPolicy)));

        AddressDataModel addressRequest = new("10 High Street", "London");
        PolicyExecutionResult result = await registry.ExecuteAsync<AddressDataModel>(
            "ADDRESS-POLICY", "corr-2", Guid.NewGuid(), addressRequest, CancellationToken.None);

        Assert.Equal("ADDRESS-POLICY", result.PolicyId);
        Assert.True(result.Passed);
    }

    // ---------------------------------------------------------------------------
    // ExecuteAsync — error paths
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task ExecuteAsync_Throws_When_PolicyId_Not_Found()
    {
        Policy<PersonDataModel> policy = MakePersonPolicy("P001");
        
        PolicyRegistry registry = BuildRegistry(
            new PolicyRegistryEntry<PersonDataModel>("P001", BuildManager(policy)));

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => registry.ExecuteAsync<PersonDataModel>("UNKNOWN", "corr-3", Guid.NewGuid(),
                new PersonDataModel("Bob", new DateOnly(1985, 5, 5)), CancellationToken.None));

        Assert.Contains("UNKNOWN", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_Throws_When_Request_Is_Wrong_Type()
    {
        Policy<PersonDataModel> policy = MakePersonPolicy("P001");
        
        PolicyRegistry registry = BuildRegistry(
            new PolicyRegistryEntry<PersonDataModel>("P001", BuildManager(policy)));

        // Pass an AddressDataModel where a PersonDataModel is expected.
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => registry.ExecuteAsync<AddressDataModel>("P001", "corr-4", Guid.NewGuid(),
                new AddressDataModel("1 Main St", "Paris"), CancellationToken.None));

        Assert.Contains(nameof(PersonDataModel), ex.Message);
    }

    // ---------------------------------------------------------------------------
    // Constructor — duplicate id detection
    // ---------------------------------------------------------------------------

    [Fact]
    public void Constructor_Throws_When_Duplicate_PolicyIds_Registered()
    {
        Policy<PersonDataModel> p1 = MakePersonPolicy("DUPE");
        Policy<PersonDataModel> p2 = MakePersonPolicy("DUPE", "Duplicate Policy");

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            BuildRegistry(
                new PolicyRegistryEntry<PersonDataModel>("DUPE", BuildManager(p1)),
                new PolicyRegistryEntry<PersonDataModel>("DUPE", BuildManager(p2))));

        Assert.Contains("DUPE", ex.Message);
    }
}
