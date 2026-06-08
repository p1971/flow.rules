using System;
using System.Collections.Generic;
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

        ILogger<MortgageApplication> logger = serviceProvider.GetRequiredService<ILogger<MortgageApplication>>();
        IPolicyManager<MortgageApplication> policyManager = serviceProvider.GetRequiredService<IPolicyManager<MortgageApplication>>();

        CancellationTokenSource cancellationTokenSource = new();
        CancellationToken cancellationToken = cancellationTokenSource.Token;

        foreach ((string name, MortgageApplication application) in GetScenarios())
        {
            LogScenario(logger, name);

            PolicyExecutionResult results = await policyManager.Execute(Guid.NewGuid().ToString(), Guid.NewGuid(), application, cancellationToken);

            LogResults(results, logger);
        }

        LogScenario(logger, "Single rule: loan-to-value only");

        RuleExecutionResult ltvResult = await policyManager.Execute(
            "MA006",
            Guid.NewGuid().ToString(),
            Guid.NewGuid(),
            CreateHighLtvApplication(),
            cancellationToken);

        LogRuleResults(logger, ltvResult.Id, ltvResult.Name, ltvResult.Passed, ltvResult.Message ?? string.Empty, ltvResult.Elapsed);

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

    private static IReadOnlyList<(string Name, MortgageApplication Application)> GetScenarios() =>
    [
        ("Standard repayment mortgage passes", CreateStandardRepaymentApplication()),
        ("Unsupported product fails", CreateStandardRepaymentApplication() with { ProductType = "OffsetTracker" }),
        ("Applicant too young fails", CreateStandardRepaymentApplication() with { ApplicantAge = 17 }),
        ("Loan outside product range fails", CreateStandardRepaymentApplication() with { DepositAmount = 330_000 }),
        ("High LTV fails", CreateHighLtvApplication()),
        ("High LTI fails", CreateStandardRepaymentApplication() with
        {
            PropertyValue = 600_000,
            DepositAmount = 100_000,
            GrossAnnualIncome = 90_000,
            NetMonthlyIncome = 7_000,
            CommittedMonthlyExpenditure = 100,
            EssentialMonthlyExpenditure = 1_000,
            Dependants = 0
        }),
        ("Affordability stress fails", CreateStandardRepaymentApplication() with { NetMonthlyIncome = 3_600 }),
        ("Term past retirement assumption fails", CreateStandardRepaymentApplication() with { ApplicantAge = 40, LoanTermYears = 35 }),
        ("Interest-only without repayment strategy fails", CreateInterestOnlyApplication() with { HasCredibleRepaymentStrategy = false, RepaymentStrategyMonthlyCost = 0 }),
        ("Multiple failures", CreateStandardRepaymentApplication() with
        {
            ApplicantAge = 17,
            DepositAmount = 10_000,
            GrossAnnualIncome = 50_000,
            NetMonthlyIncome = 2_600,
            LoanTermYears = 55
        })
    ];

    private static MortgageApplication CreateStandardRepaymentApplication() =>
        new(
            ApplicantAge: 35,
            ProductType: "ResidentialRepayment",
            PropertyValue: 350_000,
            DepositAmount: 70_000,
            GrossAnnualIncome: 85_000,
            NetMonthlyIncome: 5_000,
            CommittedMonthlyExpenditure: 300,
            EssentialMonthlyExpenditure: 1_200,
            Dependants: 1,
            LoanTermYears: 30,
            ExpectedRetirementAge: 68,
            ProductRate: 5.00m,
            StressRate: 6.25m,
            IncomeVerified: true,
            ExpenditureVerified: true);

    private static MortgageApplication CreateHighLtvApplication() =>
        CreateStandardRepaymentApplication() with { DepositAmount = 20_000 };

    private static MortgageApplication CreateInterestOnlyApplication() =>
        new(
            ApplicantAge: 45,
            ProductType: "ResidentialInterestOnly",
            PropertyValue: 600_000,
            DepositAmount: 180_000,
            GrossAnnualIncome: 120_000,
            NetMonthlyIncome: 6_500,
            CommittedMonthlyExpenditure: 500,
            EssentialMonthlyExpenditure: 1_800,
            Dependants: 0,
            LoanTermYears: 20,
            ExpectedRetirementAge: 70,
            ProductRate: 5.50m,
            StressRate: 6.50m,
            IncomeVerified: true,
            ExpenditureVerified: true,
            HasCredibleRepaymentStrategy: true,
            RepaymentStrategyMonthlyCost: 700);

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

    [LoggerMessage(LogLevel.Information, "Scenario: {scenario}")]
    private static partial void LogScenario(this ILogger<MortgageApplication> logger, string scenario);

    [LoggerMessage(LogLevel.Information, "[{id}]:[{name}] - {passed} {message} ({elapsed}ms)")]
    private static partial void LogRuleResults(this ILogger<MortgageApplication> logger, string id, string name, bool passed, string message, TimeSpan elapsed);
}
