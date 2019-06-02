using System;
using System.Collections.Generic;
using System.Linq;
using Flow.Rules.Engine.Extensions;
using Flow.Rules.Engine.Interfaces;
using Flow.Rules.Engine.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Flow.Rules.Samples.Console
{
    public static class Program
    {
        public static void Main()
        {
            ServiceCollection serviceCollection = new ServiceCollection();

            serviceCollection.AddLogging(opt =>
            {
                opt.AddConsole();
            });

            serviceCollection.AddFlowRules(GetPolicy, o => o.Lookups = GetLookups());

            ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

            IPolicyManager<MortgageApplication> policyManager = serviceProvider.GetService<IPolicyManager<MortgageApplication>>();

            MortgageApplication testMortgage = new MortgageApplication
            {
                ApplicantAge = 21,
                LoanAmount = 200_000,
                MortgageType = "FTB"
            };

            PolicyExecutionResult results = policyManager.Execute(Guid.NewGuid(), "policy1", testMortgage);

            ILogger<MortgageApplication> logger = serviceProvider.GetService<ILogger<MortgageApplication>>();

            logger.LogInformation("[{RuleContextId}]  [{PolicyId}]:[{PolicyName}] - {Passed} {Message}",
                results.RuleContextId,
                results.PolicyId,
                results.PolicyName,
                results.Passed,
                results.Message ?? string.Empty);

            if (results.RuleExecutionResults.Length > 0)
            {
                foreach (RuleExecutionResult result in results.RuleExecutionResults)
                {
                    logger.LogInformation("[{Id}]:[{Name}] - {Passed} {Message}", result.Id, result.Name, result.Passed, result.Message ?? string.Empty);
                }
            }

            serviceProvider.Dispose();
        }

        private static Policy<MortgageApplication> GetPolicy()
        {
            Rule<MortgageApplication> validMortgageTypeRule = new Rule<MortgageApplication>(
                "MA001",
                "KnownMortgageType",
                "Checks the mortgage type",
                (r) => $"The {nameof(r.MortgageType)} [{r.MortgageType}] is not known.",
                (request, lookup, calendar) =>
                {
                    ColumnResolver mortgageType = lookup["Default"][request.MortgageType];
                    return mortgageType != null;
                });

            Rule<MortgageApplication> ageLimitRule = new Rule<MortgageApplication>(
                "MA002",
                "MinAgeCheck",
                "Minimum age of the applicant",
                (r) => $"The {nameof(r.ApplicantAge)} [{r.ApplicantAge}] is too young.",
                (request, lookup, calendar) =>
                {
                    int minAgeForMortgage = lookup["Default"][request.MortgageType]["MinApplicantAge"].As<int>();
                    return request.ApplicantAge >= minAgeForMortgage;
                });

            Rule<MortgageApplication> minLoanAmountRule = new Rule<MortgageApplication>(
                "MA003",
                "MinLoanAmount",
                "minimum loan amount check",
                (r) => $"The {nameof(r.LoanAmount)} [{r.LoanAmount}] is too small.",
                (request, lookup, calendar) =>
                {
                    int minLoanAmount = lookup["Default"][request.MortgageType]["MinLoan"].As<int>();
                    return request.LoanAmount >= minLoanAmount;
                });

            Rule<MortgageApplication> maxLoanAmountRule = new Rule<MortgageApplication>(
                "MA004",
                "MaxLoanAmount",
                "Maximum loan amount check",
                (r) => $"The {nameof(r.LoanAmount)} [{r.LoanAmount}] is too large.",
                (request, lookup, calendar) =>
                {
                    int maxLoanAmount = lookup["Default"][request.MortgageType]["MaxLoan"].As<int>();
                    return request.LoanAmount <= maxLoanAmount;
                });

            Policy<MortgageApplication> policy =
                new Policy<MortgageApplication>(
                    "P001",
                    "LoanPolicy",
                    new List<Rule<MortgageApplication>>
                    {
                        validMortgageTypeRule,
                        ageLimitRule,
                        minLoanAmountRule,
                        maxLoanAmountRule
                    });

            return policy;
        }

        private static Lookups GetLookups()
        {
            Lookups lookups = new Lookups(
                new List<(string page, string row, string column, object value)>
                {
                    ("Default", "FTB", "MinLoan", 100_000),
                    ("Default", "FTB", "MaxLoan", 1_000_000),
                    ("Default", "FTB", "MinApplicantAge", 25),
                    ("Default", "BTL", "MinLoan", 200_000),
                    ("Default", "BTL", "MaxLoan", 2_000_000),
                    ("Default", "BTL", "MinApplicantAge", 30),
                }
            );

            return lookups;
        }
    }

    public class MortgageApplication
    {
        public int ApplicantAge { get; set; }
        public string MortgageType { get; set; }
        public int LoanAmount { get; set; }
    }
}
