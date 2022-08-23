using FlowRules.Engine.Models;

namespace FlowRules.Samples.TestPolicy;

public static class PolicySetup
{
    public static Policy<MortgageApplication> GetPolicy()
    {
        Lookups? lookup = GetLookups();

        Rule<MortgageApplication> validMortgageTypeRule = new(
            "MA001",
            "KnownMortgageType",
            "Checks the mortgage type",
            (r) => $"The {nameof(r.MortgageType)} [{r.MortgageType}] is not known.",
            async (request, token) =>
            {
                await Task.Delay(100, token);
                ColumnResolver mortgageType = lookup["Default"][request.MortgageType];
                return mortgageType != null;
            });

        Rule<MortgageApplication> ageLimitRule = new(
            "MA002",
            "MinAgeCheck",
            "Minimum age of the applicant",
            (r) => $"The {nameof(r.ApplicantAge)} [{r.ApplicantAge}] is too young.",
            async (request, token) =>
            {
                await Task.Delay(50, token);
                int minAgeForMortgage = lookup["Default"][request.MortgageType]["MinApplicantAge"].As<int>();
                return request.ApplicantAge >= minAgeForMortgage;
            });

        Rule<MortgageApplication> minLoanAmountRule = new(
            "MA003",
            "MinLoanAmount",
            "minimum loan amount check",
            (r) => $"The {nameof(r.LoanAmount)} [{r.LoanAmount}] is too small.",
            async (request, token) =>
            {
                await Task.Delay(20, token);
                int minLoanAmount = lookup["Default"][request.MortgageType]["MinLoan"].As<int>();
                return request.LoanAmount >= minLoanAmount;
            });

        Rule<MortgageApplication> maxLoanAmountRule = new(
            "MA004",
            "MaxLoanAmount",
            "Maximum loan amount check",
            (r) => $"The {nameof(r.LoanAmount)} [{r.LoanAmount}] is too large.",
            async (request, token) =>
            {
                await Task.Delay(20, token);
                int maxLoanAmount = lookup["Default"][request.MortgageType]["MaxLoan"].As<int>();
                return request.LoanAmount <= maxLoanAmount;
            });

        Policy<MortgageApplication> policy =
            new(
                "P001",
                "LoanPolicy",
                "Simple loan policy",
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
        Lookups lookups = new(
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
