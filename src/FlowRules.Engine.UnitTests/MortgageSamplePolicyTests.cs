using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FlowRules.Engine.Interfaces;
using FlowRules.Engine.Models;
using FlowRules.Samples.TestPolicy;

using Microsoft.Extensions.Logging;

using Xunit;
using Xunit.Abstractions;

namespace FlowRules.Engine.UnitTests;

public class MortgageSamplePolicyTests(ITestOutputHelper testOutputHelper)
{
    private readonly ILogger<PolicyManager<MortgageApplication>> _logger =
        testOutputHelper.BuildLoggerFor<PolicyManager<MortgageApplication>>();

    private readonly IFlowRulesTelemetryService _telemetryService = new NoOpFlowRulesTelemetryService();

    [Fact]
    public void GetPolicy_Should_Return_Expected_Metadata_And_RuleIds()
    {
        Policy<MortgageApplication> policy = PolicySetup.GetPolicy();

        Assert.Equal("P001", policy.Id);
        Assert.Equal("UKResidentialMortgagePolicy", policy.Name);
        Assert.Equal("Illustrative UK regulated residential mortgage lending policy.", policy.Description);
        Assert.Equal("2.0.0", policy.Version);

        Assert.Equal(
            [
                "MA001",
                "MA002",
                "MA003",
                "MA004",
                "MA005",
                "MA006",
                "MA007",
                "MA008",
                "MA009"
            ],
            policy.Rules.Select(rule => rule.Id).ToArray());
    }

    [Fact]
    public async Task Execute_Should_Pass_For_Standard_Repayment_Application()
    {
        PolicyExecutionResult result = await ExecutePolicy(CreateStandardRepaymentApplication());

        Assert.True(result.Passed);
        Assert.All(result.RuleExecutionResults, rule => Assert.True(rule.Passed));
    }

    [Fact]
    public async Task Execute_Should_Pass_For_InterestOnly_With_Repayment_Strategy()
    {
        PolicyExecutionResult result = await ExecutePolicy(CreateInterestOnlyApplication());

        Assert.True(result.Passed);
        Assert.All(result.RuleExecutionResults, rule => Assert.True(rule.Passed));
    }

    [Theory]
    [MemberData(nameof(FailingScenarios))]
    public async Task Execute_Should_Fail_Expected_Rules_For_Scenario(
        MortgageApplication application,
        string[] expectedFailedRuleIds)
    {
        PolicyExecutionResult result = await ExecutePolicy(application);

        Assert.False(result.Passed);
        Assert.Equal(expectedFailedRuleIds, GetFailedRuleIds(result));
    }

    [Fact]
    public async Task Execute_Should_Include_Calculated_Values_In_Ltv_Failure_Message()
    {
        PolicyExecutionResult result = await ExecutePolicy(CreateHighLtvApplication());
        RuleExecutionResult failedRule = Assert.Single(result.RuleExecutionResults, rule => !rule.Passed);

        Assert.Equal("MA006", failedRule.Id);
        Assert.Contains("LTV [94.3%]", failedRule.Message);
        Assert.Contains("90.0%", failedRule.Message);
    }

    [Fact]
    public async Task Execute_Should_Include_Calculated_Values_In_Lti_Failure_Message()
    {
        PolicyExecutionResult result = await ExecutePolicy(CreateHighLtiApplication());
        RuleExecutionResult failedRule = Assert.Single(result.RuleExecutionResults, rule => !rule.Passed);

        Assert.Equal("MA007", failedRule.Id);
        Assert.Contains("LTI [5.56x]", failedRule.Message);
        Assert.Contains("4.50x", failedRule.Message);
    }

    [Fact]
    public async Task Execute_Should_Include_Calculated_Values_In_Affordability_Failure_Message()
    {
        PolicyExecutionResult result = await ExecutePolicy(CreateAffordabilityFailureApplication());
        RuleExecutionResult failedRule = Assert.Single(result.RuleExecutionResults, rule => !rule.Passed);

        Assert.Equal("MA009", failedRule.Id);
        Assert.Contains("Affordability stress failed", failedRule.Message);
        Assert.Contains("Stressed monthly mortgage cost", failedRule.Message);
        Assert.Contains("required surplus", failedRule.Message);
    }

    public static TheoryData<MortgageApplication, string[]> FailingScenarios() =>
        new()
        {
            { CreateStandardRepaymentApplication() with { ProductType = "OffsetTracker" }, ["MA001"] },
            { CreateStandardRepaymentApplication() with { ApplicantAge = 17 }, ["MA002"] },
            { CreateStandardRepaymentApplication() with { IncomeVerified = false }, ["MA003"] },
            { CreateStandardRepaymentApplication() with { ApplicantAge = 40, LoanTermYears = 35 }, ["MA004"] },
            { CreateStandardRepaymentApplication() with { DepositAmount = 330_000 }, ["MA005"] },
            { CreateHighLtvApplication(), ["MA006"] },
            { CreateHighLtiApplication(), ["MA007"] },
            { CreateInterestOnlyApplication() with { HasCredibleRepaymentStrategy = false, RepaymentStrategyMonthlyCost = 0 }, ["MA008"] },
            { CreateAffordabilityFailureApplication(), ["MA009"] },
            {
                CreateStandardRepaymentApplication() with
                {
                    ApplicantAge = 17,
                    DepositAmount = 10_000,
                    GrossAnnualIncome = 50_000,
                    NetMonthlyIncome = 2_600,
                    LoanTermYears = 55
                },
                ["MA002", "MA004", "MA006", "MA007", "MA009"]
            }
        };

    private static string[] GetFailedRuleIds(PolicyExecutionResult result) =>
        result.RuleExecutionResults
            .Where(rule => !rule.Passed)
            .Select(rule => rule.Id)
            .ToArray();

    private async Task<PolicyExecutionResult> ExecutePolicy(MortgageApplication application)
    {
        Policy<MortgageApplication> policy = PolicySetup.GetPolicy();
        PolicyManager<MortgageApplication> policyManager =
            new(policy, new DefaultPolicyResultsRepository<MortgageApplication>(), _telemetryService, _logger);

        return await policyManager.Execute(
            Guid.NewGuid().ToString(),
            Guid.NewGuid(),
            application,
            CancellationToken.None);
    }

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

    private static MortgageApplication CreateHighLtiApplication() =>
        CreateStandardRepaymentApplication() with
        {
            PropertyValue = 600_000,
            DepositAmount = 100_000,
            GrossAnnualIncome = 90_000,
            NetMonthlyIncome = 7_000,
            CommittedMonthlyExpenditure = 100,
            EssentialMonthlyExpenditure = 1_000,
            Dependants = 0
        };

    private static MortgageApplication CreateAffordabilityFailureApplication() =>
        CreateStandardRepaymentApplication() with { NetMonthlyIncome = 3_600 };

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
}
