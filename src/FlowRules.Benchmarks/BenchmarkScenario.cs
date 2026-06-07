using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using FlowRules.Engine.Extensions;
using FlowRules.Engine.Interfaces;

using Microsoft.Extensions.DependencyInjection;

using RulesEngine.Models;

namespace FlowRules.Benchmarks;

internal sealed class BenchmarkScenario
{
    private BenchmarkScenario(
        IReadOnlyList<Workflow> discountWorkflows,
        RulesEngine.RulesEngine discountRulesEngine,
        DiscountCustomer discountCustomer,
        DiscountOrderHistory discountOrderHistory,
        DiscountVisitHistory discountVisitHistory,
        DiscountInput discountInput,
        IPolicyManager<DiscountInput> discountFlowRulesManager)
    {
        DiscountWorkflows = discountWorkflows;
        DiscountRulesEngine = discountRulesEngine;
        DiscountCustomer = discountCustomer;
        DiscountOrderHistory = discountOrderHistory;
        DiscountVisitHistory = discountVisitHistory;
        DiscountInput = discountInput;
        DiscountFlowRulesManager = discountFlowRulesManager;
    }

    public IReadOnlyList<Workflow> DiscountWorkflows { get; }

    public RulesEngine.RulesEngine DiscountRulesEngine { get; }

    public DiscountCustomer DiscountCustomer { get; }

    public DiscountOrderHistory DiscountOrderHistory { get; }

    public DiscountVisitHistory DiscountVisitHistory { get; }

    public DiscountInput DiscountInput { get; }

    public IPolicyManager<DiscountInput> DiscountFlowRulesManager { get; }

    public static BenchmarkScenario Create()
    {
        IReadOnlyList<Workflow> discountWorkflows = WorkflowLoader.LoadWorkflows("Discount.json");

        RulesEngine.RulesEngine discountRulesEngine = new(discountWorkflows.ToArray(), new ReSettings
        {
            EnableFormattedErrorMessage = false,
            EnableScopedParams = false
        });

        DiscountCustomer discountCustomer = new("india", 4, 100000);
        DiscountOrderHistory discountOrderHistory = new(16);
        DiscountVisitHistory discountVisitHistory = new(26);
        DiscountInput discountInput = new(discountCustomer, discountOrderHistory, discountVisitHistory);

        ServiceCollection services = new();
        services.AddLogging();
        services.AddFlowRules<DiscountInput>(
            DiscountPolicyFactory.Create,
            options => options.ExportTelemetry = false);

        IPolicyManager<DiscountInput> discountFlowRulesManager = services
            .BuildServiceProvider()
            .GetRequiredService<IPolicyManager<DiscountInput>>();

        return new BenchmarkScenario(
            discountWorkflows,
            discountRulesEngine,
            discountCustomer,
            discountOrderHistory,
            discountVisitHistory,
            discountInput,
            discountFlowRulesManager);
    }

    public void ExecuteFlowRulesDiscount(string correlationId)
    {
        _ = DiscountFlowRulesManager
            .Execute(correlationId, Guid.Empty, DiscountInput, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }
}
