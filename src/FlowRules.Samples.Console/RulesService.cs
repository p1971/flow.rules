using System;
using System.Threading;
using System.Threading.Tasks;

using FlowRules.Engine.Interfaces;
using FlowRules.Engine.Models;
using FlowRules.Samples.TestPolicy;

using Microsoft.Extensions.Logging;

namespace FlowRules.Samples.Console
{
    public class RulesService
    {
        private readonly IPolicyManager<MortgageApplication> _policyManager;
        private readonly ILogger<MortgageApplication> _logger;

        public RulesService(IPolicyManager<MortgageApplication> policyManager, ILogger<MortgageApplication> logger)
        {
            _policyManager = policyManager;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            MortgageApplication testMortgage = new(21, "FTB", 200_000);

            PolicyExecutionResult results = await _policyManager.Execute(Guid.NewGuid().ToString(), Guid.NewGuid(), testMortgage, cancellationToken);

            LogResults(results, _logger);
        }

        private static void LogResults(PolicyExecutionResult results, ILogger<MortgageApplication> logger)
        {
            logger.LogInformation("[{RuleContextId}] [{PolicyId}]:[{PolicyName}:{Version}] - {Passed}",
                results.RuleContextId,
                results.PolicyId,
                results.PolicyName,
                results.Version,
                results.Passed);

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
