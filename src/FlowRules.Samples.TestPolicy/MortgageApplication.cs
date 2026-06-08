namespace FlowRules.Samples.TestPolicy;

public record MortgageApplication(
    int ApplicantAge,
    string ProductType,
    decimal PropertyValue,
    decimal DepositAmount,
    decimal GrossAnnualIncome,
    decimal NetMonthlyIncome,
    decimal CommittedMonthlyExpenditure,
    decimal EssentialMonthlyExpenditure,
    int Dependants,
    int LoanTermYears,
    int ExpectedRetirementAge,
    decimal ProductRate,
    decimal StressRate,
    bool IncomeVerified,
    bool ExpenditureVerified,
    bool HasCredibleRepaymentStrategy = false,
    decimal RepaymentStrategyMonthlyCost = 0)
{
    public decimal LoanAmount => PropertyValue - DepositAmount;
}
