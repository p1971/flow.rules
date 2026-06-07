using System;
using System.Threading;
using System.Threading.Tasks;

using FlowRules.Engine.Extensions;
using FlowRules.Engine.Interfaces;
using FlowRules.Engine.Models;
using FlowRules.Samples.TestPolicy;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

#if SQLSERVER
using FlowRules.Extensions.SqlServer;
#endif

namespace FlowRules.Samples.Console;

public static partial class Program
{
    public static async Task Main(string[] args)
    {
        using IHost host = Host.CreateDefaultBuilder(args).Build();

        IConfiguration config = new ConfigurationBuilder()
            .AddUserSecrets(typeof(Program).Assembly)
            .AddEnvironmentVariables()
            .Build();

        ServiceCollection serviceCollection = GetServiceCollection(config);

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

        IPolicyManager<MortgageApplication> policyManager = serviceProvider.GetService<IPolicyManager<MortgageApplication>>()!;

        MortgageApplication testMortgage = new(
            21,
            "FTB",
            500_000,
            70_000,
            120_000,
            1000,
            2000,
            25);

        CancellationTokenSource cancellationTokenSource = new();
        CancellationToken cancellationToken = cancellationTokenSource.Token;

        PolicyExecutionResult results = await policyManager.Execute(Guid.NewGuid().ToString(), Guid.NewGuid(), testMortgage, cancellationToken);

        ILogger<MortgageApplication> logger = serviceProvider.GetService<ILogger<MortgageApplication>>()!;

        LogResults(results, logger);

        await host.StartAsync(cancellationToken);

        await serviceProvider.DisposeAsync();
    }

    private static ServiceCollection GetServiceCollection(IConfiguration config)
    {
        ServiceCollection serviceCollection = new();

        serviceCollection.AddLogging(opt => { opt.AddConsole(); });

#if SQLSERVER
        serviceCollection
            .AddOptions<SqlServerPolicyResultsRepositoryConfig>()
            .Bind(config.GetSection(nameof(SqlServerPolicyResultsRepositoryConfig)));
#endif

        serviceCollection.AddFlowRules<MortgageApplication>(() => PolicySetup.GetPolicy(), c =>
        {
#if SQLSERVER
            c.ResultsRepository = typeof(SqlServerPolicyResultsRepository<MortgageApplication>);
#endif
        });
        return serviceCollection;
    }

    private static void LogResults(PolicyExecutionResult results, ILogger<MortgageApplication> logger)
    {
        LogPolicyResult(logger, results.RuleContextId, results.PolicyId, results.PolicyName, results.Version, results.Passed);

        if (results.RuleExecutionResults.Length > 0)
        {
            foreach (RuleExecutionResult result in results.RuleExecutionResults)
            {
                LogRuleResults(logger, result.Id, result.Name, result.Passed, result.Message ?? string.Empty, result.Elapsed);
            }
        }
    }

    [LoggerMessage(LogLevel.Information, "[{ruleContextId}] [{policyId}]:[{policyName}:{version}] - {passed}")]
    private static partial void LogPolicyResult(this ILogger<MortgageApplication> logger, Guid ruleContextId, string? policyId, string? policyName, string? version, bool passed);


    [LoggerMessage(LogLevel.Information, "[{id}]:[{name}] - {passed} {message} ({elapsed}ms)")]
    private static partial void LogRuleResults(this ILogger<MortgageApplication> logger, string id, string name, bool passed, string message, TimeSpan elapsed);
}
