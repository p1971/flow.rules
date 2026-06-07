using System;
using System.Linq;

namespace FlowRules.Benchmarks;

public static class Program
{
    public static void Main(string[] args)
    {
        if (args.Contains("--verify", StringComparer.OrdinalIgnoreCase))
        {
            new RulesEngineComparisonBenchmark().VerifyEquivalentResults(Console.Out).GetAwaiter().GetResult();
            return;
        }

        if (args.Contains("--allocations", StringComparer.OrdinalIgnoreCase))
        {
            AllocationMeter.PrintAllocationBreakdown(new RulesEngineComparisonBenchmark(), Console.Out);
            return;
        }

        _ = BenchmarkDotNet.Running.BenchmarkRunner.Run<RulesEngineComparisonBenchmark>();
    }
}
