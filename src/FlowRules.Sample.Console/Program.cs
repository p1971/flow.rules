using System;
using System.Threading;
using System.Threading.Tasks;

using FlowRules.Engine.Extensions;
using FlowRules.Engine.Interfaces;
using FlowRules.Engine.Models;
using FlowRules.Samples.TestPolicy;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FlowRules.Samples.Console
{
    public static class Program
    {
        public static async Task Main()
        {
            ServiceCollection serviceCollection = new();

            serviceCollection.AddLogging(opt =>
            {
                opt.AddConsole();
            });

            serviceCollection.AddFlowRules<MortgageApplication>(() => PolicySetup.GetPolicy());

            ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

            IPolicyManager<MortgageApplication> policyManager = serviceProvider.GetService<IPolicyManager<MortgageApplication>>();

            ILogger<MortgageApplication> logger = serviceProvider.GetService<ILogger<MortgageApplication>>();

            MortgageApplication testMortgage = new(21, "FTB", 200_000);

            CancellationTokenSource cancellationTokenSource = new();
            CancellationToken cancellationToken = cancellationTokenSource.Token;


            PolicyExecutionResult results = await policyManager.Execute(Guid.NewGuid(), testMortgage, cancellationToken);

            LogResults(results, logger);

            serviceProvider.Dispose();
        }

        private static void LogResults(PolicyExecutionResult results, ILogger<MortgageApplication> logger)
        {
            logger.LogInformation("[{RuleContextId}]  [{PolicyId}]:[{PolicyName}:{Version}] - {Passed} {Message}",
                results.RuleContextId,
                results.PolicyId,
                results.PolicyName,
                results.Version,
                results.Passed,
                results.Message ?? string.Empty);

            if (results.RuleExecutionResults.Length > 0)
            {
                foreach (RuleExecutionResult result in results.RuleExecutionResults)
                {
                    logger.LogInformation("[{Id}]:[{Name}] - {Passed} {Message} ({Elapsed}ms)", result.Id, result.Name, result.Passed, result.Message ?? string.Empty, result.Elapsed);
                }
            }
        }
    }
}
