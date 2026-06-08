using System.Globalization;

using FlowRules.Engine;
using FlowRules.Engine.Models;

namespace FlowRules.Samples.TestPolicy;

public static class PolicySetup
{
    private const string ProductRoot = "Products";
    private const string PolicyRoot = "Policy";
    private const string RepaymentProductType = "ResidentialRepayment";
    private const string InterestOnlyProductType = "ResidentialInterestOnly";

    public static Policy<MortgageApplication> GetPolicy()
    {
        NestedLookup<string, object> lookup = GetLookups();

        return PolicyBuilder<MortgageApplication>.Create()
            .WithId("P001")
            .WithName("UKResidentialMortgagePolicy")
            .WithDescription("Illustrative UK regulated residential mortgage lending policy.")
            .WithVersion("2.0.0")
            .WithRule(
                "MA001",
                "SupportedProductType",
                Rule(async (request, token) =>
                {
                    await Task.Delay(5, token);
                    return IsSupportedProductType(lookup, request);
                }),
                description: "Checks that the application is for a supported UK residential mortgage product.",
                failureMessage: request => $"The product type [{request.ProductType}] is not supported. Supported types are [{RepaymentProductType}, {InterestOnlyProductType}].")
            .WithRule(
                "MA002",
                "MinimumApplicantAge",
                Rule(async (request, token) =>
                {
                    await Task.Delay(5, token);
                    int minAge = lookup[PolicyRoot]["MinApplicantAge"];
                    return request.ApplicantAge >= minAge;
                }),
                description: "Checks the applicant meets the sample lender minimum age.",
                failureMessage: request =>
                {
                    int minAge = lookup[PolicyRoot]["MinApplicantAge"];
                    return $"Applicant age [{request.ApplicantAge}] is below the sample lender minimum age [{minAge}].";
                })
            .WithRule(
                "MA003",
                "VerifiedIncomeAndExpenditure",
                Rule(async (request, token) =>
                {
                    await Task.Delay(5, token);
                    return request.IncomeVerified
                        && request.ExpenditureVerified
                        && request.GrossAnnualIncome > 0
                        && request.NetMonthlyIncome > 0;
                }),
                description: "Checks that income and expenditure have been verified before affordability assessment.",
                failureMessage: request =>
                    $"Income verified [{request.IncomeVerified}], expenditure verified [{request.ExpenditureVerified}], gross annual income [GBP {Money(request.GrossAnnualIncome)}], net monthly income [GBP {Money(request.NetMonthlyIncome)}].")
            .WithRule(
                "MA004",
                "TermWithinPolicy",
                Rule(async (request, token) =>
                {
                    await Task.Delay(5, token);
                    int minTermYears = lookup[PolicyRoot]["MinTermYears"];
                    int maxTermYears = lookup[PolicyRoot]["MaxTermYears"];
                    int ageAtEndOfTerm = request.ApplicantAge + request.LoanTermYears;

                    return request.LoanTermYears >= minTermYears
                        && request.LoanTermYears <= maxTermYears
                        && request.ExpectedRetirementAge > request.ApplicantAge
                        && ageAtEndOfTerm <= request.ExpectedRetirementAge;
                }),
                description: "Checks the mortgage term and whether it extends beyond the declared retirement age.",
                failureMessage: request =>
                {
                    int minTermYears = lookup[PolicyRoot]["MinTermYears"];
                    int maxTermYears = lookup[PolicyRoot]["MaxTermYears"];
                    int ageAtEndOfTerm = request.ApplicantAge + request.LoanTermYears;

                    return $"Loan term [{request.LoanTermYears}] years must be between [{minTermYears}] and [{maxTermYears}] years and end by retirement age [{request.ExpectedRetirementAge}]. Applicant age at term end is [{ageAtEndOfTerm}].";
                })
            .WithRule(
                "MA005",
                "LoanAmountAndDeposit",
                Rule(async (request, token) =>
                {
                    await Task.Delay(5, token);

                    if (request.PropertyValue <= 0 || request.DepositAmount <= 0 || request.LoanAmount <= 0)
                    {
                        return false;
                    }

                    if (!IsSupportedProductType(lookup, request))
                    {
                        return true;
                    }

                    decimal minLoanAmount = lookup[ProductRoot][request.ProductType]["MinLoanAmount"];
                    decimal maxLoanAmount = lookup[ProductRoot][request.ProductType]["MaxLoanAmount"];

                    return request.LoanAmount >= minLoanAmount && request.LoanAmount <= maxLoanAmount;
                }),
                description: "Checks the deposit creates a positive loan amount within sample product limits.",
                failureMessage: request =>
                {
                    decimal minLoanAmount = IsSupportedProductType(lookup, request)
                        ? lookup[ProductRoot][request.ProductType]["MinLoanAmount"]
                        : 0;
                    decimal maxLoanAmount = IsSupportedProductType(lookup, request)
                        ? lookup[ProductRoot][request.ProductType]["MaxLoanAmount"]
                        : 0;

                    return $"Property value [GBP {Money(request.PropertyValue)}], deposit [GBP {Money(request.DepositAmount)}], and loan amount [GBP {Money(request.LoanAmount)}] must create a positive loan within the sample product range [GBP {Money(minLoanAmount)} - GBP {Money(maxLoanAmount)}].";
                })
            .WithRule(
                "MA006",
                "LoanToValue",
                Rule(async (request, token) =>
                {
                    await Task.Delay(5, token);

                    if (!IsSupportedProductType(lookup, request) || !CanCalculateLoanRatios(request))
                    {
                        return true;
                    }

                    decimal ltv = CalculateLoanToValue(request);
                    decimal maxLtv = lookup[ProductRoot][request.ProductType]["MaxLoanToValue"];

                    return ltv <= maxLtv;
                }),
                description: "Checks loan-to-value against the sample product limit.",
                failureMessage: request =>
                {
                    decimal ltv = CanCalculateLoanRatios(request) ? CalculateLoanToValue(request) : 0;
                    decimal maxLtv = IsSupportedProductType(lookup, request)
                        ? lookup[ProductRoot][request.ProductType]["MaxLoanToValue"]
                        : 0;

                    return $"LTV [{Percent(ltv)}] exceeds the sample product limit [{Percent(maxLtv)}].";
                })
            .WithRule(
                "MA007",
                "LoanToIncome",
                Rule(async (request, token) =>
                {
                    await Task.Delay(5, token);

                    if (!IsSupportedProductType(lookup, request) || request.GrossAnnualIncome <= 0 || request.LoanAmount <= 0)
                    {
                        return true;
                    }

                    decimal lti = CalculateLoanToIncome(request);
                    decimal maxLti = lookup[PolicyRoot]["MaxLoanToIncome"];

                    return lti <= maxLti;
                }),
                description: "Checks loan-to-income against an illustrative sample lender cap.",
                failureMessage: request =>
                {
                    decimal lti = request.GrossAnnualIncome > 0 ? CalculateLoanToIncome(request) : 0;
                    decimal maxLti = lookup[PolicyRoot]["MaxLoanToIncome"];

                    return $"LTI [{Ratio(lti)}] exceeds the sample lender limit [{Ratio(maxLti)}]. The UK high-LTI flow limit is a portfolio control, so this sample treats the threshold as lender policy.";
                })
            .WithRule(
                "MA008",
                "InterestOnlyRepaymentStrategy",
                Rule(async (request, token) =>
                {
                    await Task.Delay(5, token);

                    if (!IsInterestOnlyProduct(request))
                    {
                        return true;
                    }

                    return request.HasCredibleRepaymentStrategy && request.RepaymentStrategyMonthlyCost > 0;
                }),
                description: "Checks interest-only applications have a credible repayment strategy and monthly strategy cost.",
                failureMessage: request =>
                    $"Interest-only applications need a credible repayment strategy and strategy cost. Strategy present [{request.HasCredibleRepaymentStrategy}], monthly strategy cost [GBP {Money(request.RepaymentStrategyMonthlyCost)}].")
            .WithRule(
                "MA009",
                "AffordabilityStress",
                Rule(async (request, token) =>
                {
                    await Task.Delay(5, token);

                    if (!CanAssessAffordability(lookup, request))
                    {
                        return true;
                    }

                    decimal minimumMonthlySurplus = lookup[PolicyRoot]["MinimumMonthlySurplus"];
                    decimal surplus = CalculateMonthlySurplus(lookup, request);

                    return surplus >= minimumMonthlySurplus;
                }),
                description: "Checks affordability using verified net income, committed expenditure, household costs, dependants, and a stressed mortgage payment.",
                failureMessage: request =>
                {
                    decimal effectiveStressRate = CalculateEffectiveStressRate(lookup, request);
                    decimal mortgagePayment = CalculateStressedMortgagePayment(lookup, request);
                    decimal surplus = CalculateMonthlySurplus(lookup, request);
                    decimal minimumMonthlySurplus = lookup[PolicyRoot]["MinimumMonthlySurplus"];

                    return $"Affordability stress failed at [{Percent(effectiveStressRate)}]. Stressed monthly mortgage cost [GBP {Money(mortgagePayment)}], monthly surplus [GBP {Money(surplus)}], required surplus [GBP {Money(minimumMonthlySurplus)}].";
                })
            .Build();
    }

    private static Func<MortgageApplication, CancellationToken, Task<bool>> Rule(
        Func<MortgageApplication, CancellationToken, Task<bool>> source) => source;

    private static NestedLookup<string, object> GetLookups()
    {
        List<(IEnumerable<string> Keys, object Value)> data =
        [
            (["Policy", "MinApplicantAge"], 18),
            (["Policy", "MinTermYears"], 5),
            (["Policy", "MaxTermYears"], 40),
            (["Policy", "MinimumStressRateIncrease"], 1.0m),
            (["Policy", "MinimumMonthlySurplus"], 250m),
            (["Policy", "DependantMonthlyAllowance"], 250m),
            (["Policy", "MaxLoanToIncome"], 4.50m),

            (["Products", RepaymentProductType, "MinLoanAmount"], 25_000m),
            (["Products", RepaymentProductType, "MaxLoanAmount"], 1_500_000m),
            (["Products", RepaymentProductType, "MaxLoanToValue"], 90.0m),

            (["Products", InterestOnlyProductType, "MinLoanAmount"], 50_000m),
            (["Products", InterestOnlyProductType, "MaxLoanAmount"], 1_000_000m),
            (["Products", InterestOnlyProductType, "MaxLoanToValue"], 75.0m),
        ];

        return new NestedLookup<string, object>(data);
    }

    private static bool IsSupportedProductType(NestedLookup<string, object> lookup, MortgageApplication request) =>
        !string.IsNullOrWhiteSpace(request.ProductType) && lookup[ProductRoot].IsDefined(request.ProductType);

    private static bool IsInterestOnlyProduct(MortgageApplication request) =>
        string.Equals(request.ProductType, InterestOnlyProductType, StringComparison.Ordinal);

    private static bool CanCalculateLoanRatios(MortgageApplication request) =>
        request.PropertyValue > 0 && request.LoanAmount > 0;

    private static bool CanAssessAffordability(NestedLookup<string, object> lookup, MortgageApplication request) =>
        IsSupportedProductType(lookup, request)
        && CanCalculateLoanRatios(request)
        && request.LoanTermYears > 0
        && request.NetMonthlyIncome > 0
        && request.IncomeVerified
        && request.ExpenditureVerified
        && (!IsInterestOnlyProduct(request) || request.HasCredibleRepaymentStrategy);

    private static decimal CalculateLoanToValue(MortgageApplication request) =>
        request.LoanAmount / request.PropertyValue * 100;

    private static decimal CalculateLoanToIncome(MortgageApplication request) =>
        request.LoanAmount / request.GrossAnnualIncome;

    private static decimal CalculateEffectiveStressRate(NestedLookup<string, object> lookup, MortgageApplication request)
    {
        decimal minimumStressRateIncrease = lookup[PolicyRoot]["MinimumStressRateIncrease"];
        decimal productRatePlusMinimumIncrease = request.ProductRate + minimumStressRateIncrease;

        return Math.Max(request.StressRate, productRatePlusMinimumIncrease);
    }

    private static decimal CalculateStressedMortgagePayment(NestedLookup<string, object> lookup, MortgageApplication request)
    {
        decimal stressRate = CalculateEffectiveStressRate(lookup, request);

        if (IsInterestOnlyProduct(request))
        {
            return (request.LoanAmount * stressRate / 100 / 12) + request.RepaymentStrategyMonthlyCost;
        }

        return CalculateMonthlyRepayment(stressRate, request.LoanTermYears * 12, request.LoanAmount);
    }

    private static decimal CalculateMonthlySurplus(NestedLookup<string, object> lookup, MortgageApplication request)
    {
        decimal dependantAllowance = lookup[PolicyRoot]["DependantMonthlyAllowance"];
        decimal stressedMortgagePayment = CalculateStressedMortgagePayment(lookup, request);
        decimal householdExpenditure =
            request.CommittedMonthlyExpenditure
            + request.EssentialMonthlyExpenditure
            + (request.Dependants * dependantAllowance);

        return request.NetMonthlyIncome - householdExpenditure - stressedMortgagePayment;
    }

    private static decimal CalculateMonthlyRepayment(decimal annualRatePercentage, int numberOfPayments, decimal loanAmount)
    {
        if (numberOfPayments <= 0)
        {
            return 0;
        }

        decimal monthlyRate = annualRatePercentage / 100 / 12;

        if (monthlyRate == 0)
        {
            return loanAmount / numberOfPayments;
        }

        double rate = (double)monthlyRate;
        double compoundFactor = Math.Pow(1 + rate, numberOfPayments);
        double payment = (double)loanAmount * rate * compoundFactor / (compoundFactor - 1);

        return (decimal)payment;
    }

    private static string Money(decimal value) =>
        value.ToString("N2", CultureInfo.InvariantCulture);

    private static string Percent(decimal value) =>
        value.ToString("0.0", CultureInfo.InvariantCulture) + "%";

    private static string Ratio(decimal value) =>
        value.ToString("0.00", CultureInfo.InvariantCulture) + "x";
}
