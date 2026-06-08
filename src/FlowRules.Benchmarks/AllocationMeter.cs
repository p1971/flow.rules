using System;
using System.IO;

using FlowRules.Engine.Models;

namespace FlowRules.Benchmarks;

internal static class AllocationMeter
{
    public static void PrintAllocationBreakdown(RulesEngineComparisonBenchmark benchmark, TextWriter writer)
    {
        const int iterations = 100_000;

        benchmark.WarmUpFlowRulesDiscount();

        long flowRulesDiscountBytes = MeasureAllocatedBytes(iterations, benchmark.ExecuteFlowRulesDiscountOnce);
        long discountResultShapeBytes = MeasureAllocatedBytes(iterations, static () => _ = CreateResultShape(6));

        writer.WriteLine($"Iterations: {iterations}");
        WriteAllocationLine(writer, "FlowRulesDiscount Execute", flowRulesDiscountBytes, iterations);
        WriteAllocationLine(writer, "Discount result objects only", discountResultShapeBytes, iterations);
    }

    private static PolicyExecutionResult CreateResultShape(int ruleCount)
    {
        RuleExecutionResult[] ruleResults = new RuleExecutionResult[ruleCount];

        for (int index = 0; index < ruleResults.Length; index++)
        {
            ruleResults[index] = new RuleExecutionResult(
                "RuleId",
                "RuleName",
                true,
                TimeSpan.Zero,
                null,
                null,
                null);
        }

        return new PolicyExecutionResult
        {
            RuleContextId = Guid.Empty,
            CorrelationId = "measure",
            PolicyId = "PolicyId",
            PolicyName = "PolicyName",
            Version = "Version",
            RuleExecutionResults = ruleResults,
            Passed = true
        };
    }

    private static long MeasureAllocatedBytes(int iterations, Action action)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int iteration = 0; iteration < iterations; iteration++)
        {
            action();
        }

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    private static void WriteAllocationLine(TextWriter writer, string name, long bytes, int iterations)
    {
        writer.WriteLine($"{name}: {bytes:N0} B total, {bytes / (double)iterations:N1} B/execution");
    }
}
