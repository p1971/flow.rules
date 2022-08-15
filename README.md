# flow rules ![MIT](https://badgen.net/badge/license/MIT/green)

A simple rules implementation for dotnet.

## CI


| Project | Build (master) |  Build (develop) |
|---------|-------|-------|
| [FlowRules](https://github.com/p1971/flow.rules.engine) | ![master](https://github.com/p1971/flow.rules.engine/workflows/flowrules_build/badge.svg?branch=master) |  ![develop](https://github.com/p1971/flow.rules.engine/workflows/flowrules_build/badge.svg?branch=develop) |

## Testing

[![codecov](https://codecov.io/gh/p1971/flow.rules/branch/develop/graph/badge.svg?token=WEFAEQe92g)](https://codecov.io/gh/p1971/flow.rules)

## Packaging

Packages from feature and develop branches are available on [github](https://github.com/p1971?tab=packages&repo_name=flow.rules).

Release and symbol packages are available on [nuget.org](https://nuget.org).

| Nuget | Description | Nuget |
| ------| ------- | ------ |
| FlowRules.Engine | Core engine | [![nuget](https://img.shields.io/nuget/v/FlowRules.svg)](https://www.nuget.org/packages/FlowRules) |
| FlowRules.WebApi | Adds a webapi controller for executing flows | [![nuget](https://img.shields.io/nuget/v/FlowRules.WebApi.svg)](https://www.nuget.org/packages/FlowRules.WebApi) |
| FlowRules.Extensions.SqlServer | Sql server support | [![nuget](https://img.shields.io/nuget/v/FlowRules.Extensions.SqlServer.svg)](https://www.nuget.org/packages/FlowRules.Extensions.SqlServer) |

## Getting started

### A simple dto to apply rules to.

```csharp
public record MortgageApplication(int ApplicantAge, string MortgageType, int LoanAmount);
```

### The rules policy

```csharp

public class PolicyBuilder 
{
    public Policy<MortgageApplication> GetPolicy()
    {
     Rule<MortgageApplication> validMortgageTypeRule = new(
        "MA001",
        "KnownMortgageType",
        "Checks the mortgage type",
        (r) => $"The {nameof(r.MortgageType)} [{r.MortgageType}] is not known.",
        (request, token) =>
        {
            ColumnResolver mortgageType = lookup["Default"][request.MortgageType];
            return Task.FromResult(mortgageType != null);
        });

    Rule<MortgageApplication> ageLimitRule = new(
        "MA002",
        "MinAgeCheck",
        "Minimum age of the applicant",
        (r) => $"The {nameof(r.ApplicantAge)} [{r.ApplicantAge}] is too young.",
        (request, token) =>
        {
            int minAgeForMortgage = lookup["Default"][request.MortgageType]["MinApplicantAge"].As<int>();
            return Task.FromResult(request.ApplicantAge >= minAgeForMortgage);
        });

    Policy<MortgageApplication> policy = new(
        "P001",
        "LoanPolicy",
        new List<Rule<MortgageApplication>>
        {
            validMortgageTypeRule,
            ageLimitRule,        
        });
       return policy;
    }
}
```

### Registering the services and executing a request.
```csharp
ServiceCollection serviceCollection = new();

serviceCollection.AddFlowRules<MortgageApplication>(() => policy);

ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

IPolicyManager<MortgageApplication> policyManager = serviceProvider.GetService<IPolicyManager<MortgageApplication>>();

MortgageApplication testMortgage = new(21, "FTB", 200_000);

CancellationTokenSource cancellationTokenSource = new();
CancellationToken cancellationToken = cancellationTokenSource.Token;

PolicyExecutionResult results = await policyManager.Execute(Guid.NewGuid(), testMortgage, cancellationToken);
```

## Authors

- Pete Robinson
