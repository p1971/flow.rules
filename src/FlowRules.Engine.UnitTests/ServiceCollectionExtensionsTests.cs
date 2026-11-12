using System;
using System.Threading.Tasks;

using FlowRules.Engine.Extensions;
using FlowRules.Engine.Interfaces;
using FlowRules.Engine.Models;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Xunit;
using Xunit.Abstractions;

namespace FlowRules.Engine.UnitTests;

public class ServiceCollectionExtensionsTests
{
    private readonly IServiceCollection _subject = new ServiceCollection();

    public ServiceCollectionExtensionsTests(ITestOutputHelper testOutputHelper)
    {
        _subject.AddSingleton<ILogger<PolicyManager<PersonDataModel>>>(
            new TestLogger<PolicyManager<PersonDataModel>>(testOutputHelper));
    }

    [Fact]
    public void AddFlowRules_Should_Add_Default_ResultsRepository()
    {
        Policy<PersonDataModel> policy = GetTestPolicy("P001");

        _subject.AddFlowRules<PersonDataModel>(() => policy);

        ServiceProvider serviceProvider = _subject.BuildServiceProvider();

        AssertPolicyRegistered(serviceProvider, "P001", typeof(DefaultPolicyResultsRepository<PersonDataModel>));
    }

    [Fact]
    public void AddFlowRules_Should_Add_Custom_ResultsRepository()
    {
        Policy<PersonDataModel> policy = GetTestPolicy("P001");

        _subject.AddFlowRules(
            () => policy,
            o => o.ResultsRepository = typeof(TrackingResultsRepository));

        ServiceDescriptor descriptor = Assert.Single(
            _subject,
            item => item.ServiceType == typeof(IPolicyResultsRepository<PersonDataModel>));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);

        ServiceProvider serviceProvider = _subject.BuildServiceProvider();

        AssertPolicyRegistered(serviceProvider, "P001", typeof(TrackingResultsRepository));
    }

    [Theory]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void AddFlowRules_Should_Preserve_Application_Repository_Lifetime(ServiceLifetime lifetime)
    {
        Policy<PersonDataModel> policy = GetTestPolicy("P001");
        _subject.Add(new ServiceDescriptor(
            typeof(IPolicyResultsRepository<PersonDataModel>),
            typeof(TrackingResultsRepository),
            lifetime));

        _subject.AddFlowRules<PersonDataModel>(() => policy);

        ServiceDescriptor descriptor = Assert.Single(
            _subject,
            item => item.ServiceType == typeof(IPolicyResultsRepository<PersonDataModel>));
        Assert.Equal(lifetime, descriptor.Lifetime);
    }

    [Fact]
    public void AddFlowRules_Should_Resolve_Scoped_Manager_With_Scoped_Repository()
    {
        Policy<PersonDataModel> policy = GetTestPolicy("P001");
        _subject.AddScoped<IPolicyResultsRepository<PersonDataModel>, TrackingResultsRepository>();
        _subject.AddFlowRules<PersonDataModel>(() => policy);

        using ServiceProvider serviceProvider = _subject.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        IPolicyManager<PersonDataModel> manager1;
        IPolicyResultsRepository<PersonDataModel> repository1;
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            manager1 = scope.ServiceProvider.GetRequiredKeyedService<IPolicyManager<PersonDataModel>>("P001");
            Assert.Same(
                manager1,
                scope.ServiceProvider.GetRequiredKeyedService<IPolicyManager<PersonDataModel>>("P001"));

            repository1 = scope.ServiceProvider.GetRequiredService<IPolicyResultsRepository<PersonDataModel>>();
            Assert.Same(
                repository1,
                scope.ServiceProvider.GetRequiredService<IPolicyResultsRepository<PersonDataModel>>());
        }

        using IServiceScope secondScope = serviceProvider.CreateScope();
        Assert.NotSame(
            manager1,
            secondScope.ServiceProvider.GetRequiredKeyedService<IPolicyManager<PersonDataModel>>("P001"));
        Assert.NotSame(
            repository1,
            secondScope.ServiceProvider.GetRequiredService<IPolicyResultsRepository<PersonDataModel>>());
    }

    [Fact]
    public void AddFlowRules_Should_Add_Telemetry_By_Default()
    {
        Policy<PersonDataModel> policy = GetTestPolicy("P001");

        _subject.AddFlowRules<PersonDataModel>(() => policy);

        ServiceProvider serviceProvider = _subject.BuildServiceProvider();

        IFlowRulesTelemetryService telemetryService =
            serviceProvider.GetRequiredService<IFlowRulesTelemetryService>();

        Assert.IsType<FlowRulesTelemetryService>(telemetryService);
    }

    [Fact]
    public void AddFlowRules_Should_Add_NoOp_Telemetry_When_Disabled()
    {
        Policy<PersonDataModel> policy = GetTestPolicy("P001");

        _subject.AddFlowRules<PersonDataModel>(
            () => policy,
            options => options.ExportTelemetry = false);

        ServiceProvider serviceProvider = _subject.BuildServiceProvider();

        IFlowRulesTelemetryService telemetryService =
            serviceProvider.GetRequiredService<IFlowRulesTelemetryService>();

        Assert.IsType<NoOpFlowRulesTelemetryService>(telemetryService);
    }

    [Fact]
    public void AddFlowRules_Should_Replace_Telemetry_When_Disabled_After_Default_Registration()
    {
        Policy<PersonDataModel> policy1 = GetTestPolicy("P001");
        Policy<PersonDataModel> policy2 = GetTestPolicy("P002");

        _subject.AddFlowRules<PersonDataModel>(() => policy1);
        _subject.AddFlowRules<PersonDataModel>(
            () => policy2,
            options => options.ExportTelemetry = false);

        ServiceProvider serviceProvider = _subject.BuildServiceProvider();

        IFlowRulesTelemetryService telemetryService =
            serviceProvider.GetRequiredService<IFlowRulesTelemetryService>();

        Assert.IsType<NoOpFlowRulesTelemetryService>(telemetryService);
    }

    [Fact]
    public void AddFlowRules_Should_Support_Multiple_Policies_For_Same_Type()
    {
        Policy<PersonDataModel> policy1 = GetTestPolicy("P001");
        Policy<PersonDataModel> policy2 = GetTestPolicy("P002");

        _subject.AddFlowRules<PersonDataModel>(() => policy1);
        _subject.AddFlowRules<PersonDataModel>(() => policy2);

        ServiceProvider serviceProvider = _subject.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        // Both policies should be independently resolvable by id.
        Policy<PersonDataModel> resolved1 = serviceProvider.GetRequiredKeyedService<Policy<PersonDataModel>>("P001");
        Policy<PersonDataModel> resolved2 = serviceProvider.GetRequiredKeyedService<Policy<PersonDataModel>>("P002");

        Assert.Equal("P001", resolved1.Id);
        Assert.Equal("P002", resolved2.Id);

        // Each keyed manager should be backed by its own policy.
        IPolicyManager<PersonDataModel> manager1 = scope.ServiceProvider.GetRequiredKeyedService<IPolicyManager<PersonDataModel>>("P001");
        IPolicyManager<PersonDataModel> manager2 = scope.ServiceProvider.GetRequiredKeyedService<IPolicyManager<PersonDataModel>>("P002");

        Assert.NotSame(manager1, manager2);
    }

    [Fact]
    public void AddFlowRulesRegistry_Should_Register_All_Policy_Entries()
    {
        Policy<PersonDataModel> policy1 = GetTestPolicy("P001");
        Policy<PersonDataModel> policy2 = GetTestPolicy("P002");

        _subject.AddFlowRules<PersonDataModel>(() => policy1);
        _subject.AddFlowRules<PersonDataModel>(() => policy2);
        _subject.AddFlowRulesRegistry();

        ServiceProvider serviceProvider = _subject.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using IServiceScope scope = serviceProvider.CreateScope();

        IPolicyRegistry registry = scope.ServiceProvider.GetRequiredService<IPolicyRegistry>();

        Assert.Equal(2, registry.PolicyIds.Count);
        Assert.Contains("P001", registry.PolicyIds);
        Assert.Contains("P002", registry.PolicyIds);
    }

    private static void AssertPolicyRegistered(
        ServiceProvider serviceProvider,
        string policyId,
        Type resultsRepositoryType)
    {
        using IServiceScope scope = serviceProvider.CreateScope();

        // Policy<T> is now keyed — resolve by id.
        Policy<PersonDataModel> policy = serviceProvider.GetRequiredKeyedService<Policy<PersonDataModel>>(policyId);
        Assert.NotNull(policy);
        Assert.Equal(policyId, policy.Id);

        IPolicyResultsRepository<PersonDataModel> repo =
            scope.ServiceProvider.GetRequiredService<IPolicyResultsRepository<PersonDataModel>>();
        Assert.Equal(resultsRepositoryType, repo.GetType());

        // Unkeyed IPolicyManager<T> must still resolve for backward compatibility.
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IPolicyManager<PersonDataModel>>());
    }

    private static Policy<PersonDataModel> GetTestPolicy(string id)
    {
        return PolicyBuilder<PersonDataModel>.Create()
            .WithId(id)
            .WithName($"test policy {id}")
            .WithDescription("policy description")
            .WithRule("R001", "test rule", (model, token) => Task.FromResult(true), description: "test description")
            .Build();
    }

    private sealed class TrackingResultsRepository : IPolicyResultsRepository<PersonDataModel>
    {
        public ValueTask PersistResults(
            PersonDataModel request,
            PolicyExecutionResult policyExecutionResult) =>
            ValueTask.CompletedTask;
    }
}
