using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;

using FlowRules.Engine.Models;

using RulesEngine.Models;

namespace FlowRules.Benchmarks;

[MemoryDiagnoser]
[InProcess]
public class RulesEngineComparisonBenchmark
{
    private readonly BenchmarkScenario _scenario = BenchmarkScenario.Create();

    [Params(1, 10, 1000, 10000)]
    public int N;

    [Benchmark(Baseline = true)]
    public void MicrosoftRulesEngineDiscount()
    {
        for (int iteration = 0; iteration < N; iteration++)
        {
            foreach (Workflow workflow in _scenario.DiscountWorkflows)
            {
                _ = _scenario.DiscountRulesEngine
                    .ExecuteAllRulesAsync(
                        workflow.WorkflowName,
                        _scenario.DiscountCustomer,
                        _scenario.DiscountOrderHistory,
                        _scenario.DiscountVisitHistory)
                    .GetAwaiter()
                    .GetResult();
            }
        }
    }

    [Benchmark]
    public void FlowRulesDiscount()
    {
        for (int iteration = 0; iteration < N; iteration++)
        {
            ExecuteFlowRulesDiscountOnce();
        }
    }

    public void ExecuteFlowRulesDiscountOnce()
    {
        _scenario.ExecuteFlowRulesDiscount("benchmark");
    }

    public void WarmUpFlowRulesDiscount()
    {
        _scenario.ExecuteFlowRulesDiscount("warmup");
    }

    public async Task VerifyEquivalentResults(TextWriter writer)
    {
        foreach (Workflow workflow in _scenario.DiscountWorkflows)
        {
            RuleResultTree[] microsoftResults = (await _scenario.DiscountRulesEngine
                .ExecuteAllRulesAsync(
                    workflow.WorkflowName,
                    _scenario.DiscountCustomer,
                    _scenario.DiscountOrderHistory,
                    _scenario.DiscountVisitHistory))
                .ToArray();
            PolicyExecutionResult flowRulesResult = await _scenario.DiscountFlowRulesManager.Execute(
                "verify",
                Guid.Empty,
                _scenario.DiscountInput,
                CancellationToken.None);

            VerifyResults(workflow.WorkflowName, microsoftResults, flowRulesResult, writer);
        }
    }

    private static void VerifyResults(
        string workflowName,
        IReadOnlyCollection<RuleResultTree> microsoftResults,
        PolicyExecutionResult flowRulesResult,
        TextWriter writer)
    {
        Dictionary<string, bool> microsoftByRuleName = microsoftResults.ToDictionary(r => r.Rule.RuleName, r => r.IsSuccess);
        Dictionary<string, bool> flowRulesByRuleName = flowRulesResult.RuleExecutionResults.ToDictionary(r => r.Name, r => r.Passed);

        if (!microsoftByRuleName.Keys.OrderBy(static key => key).SequenceEqual(flowRulesByRuleName.Keys.OrderBy(static key => key)))
        {
            throw new InvalidOperationException(
                $"Rule names differ for workflow {workflowName}. Microsoft: [{string.Join(", ", microsoftByRuleName.Keys)}]. Flow.Rules: [{string.Join(", ", flowRulesByRuleName.Keys)}].");
        }

        foreach ((string ruleName, bool microsoftPassed) in microsoftByRuleName.OrderBy(static pair => pair.Key))
        {
            bool flowRulesPassed = flowRulesByRuleName[ruleName];
            if (microsoftPassed != flowRulesPassed)
            {
                throw new InvalidOperationException(
                    $"Rule result mismatch for workflow {workflowName}, rule {ruleName}. Microsoft RulesEngine: {microsoftPassed}. Flow.Rules: {flowRulesPassed}.");
            }

            writer.WriteLine($"{workflowName}.{ruleName}: {microsoftPassed}");
        }
    }
}
