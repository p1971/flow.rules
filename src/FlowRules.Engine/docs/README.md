# FlowRules.Engine

A lightweight, strongly-typed rules engine for .NET 8 and .NET 10.

Define business policies as composable, async rules against any request model, execute them via dependency injection, and get a structured result per rule and per policy - with built-in OpenTelemetry traces, metrics, and logs.

---

## Features

- **Strongly-typed policies** - each `Policy<T>` is bound to a specific request model; no casting or reflection at runtime.
- **Fluent builder** - compose policies and rules with `PolicyBuilder<T>`.
- **Async rules** - every rule predicate is `Func<T, CancellationToken, ValueTask<bool>>`; `Task<bool>` rules are also supported for compatibility.
- **Structured results** - `PolicyExecutionResult` captures pass/fail at both policy and individual rule level, including optional dynamic failure messages.
- **Single-rule execution** - execute a specific rule by id for targeted validation scenarios.
- **Multi-policy registry** - `IPolicyRegistry` manages policies across many DTO types and dispatches execution by policy id without requiring a separately injected `IPolicyManager<T>` per policy.
- **Result persistence** - optional `IPolicyResultsRepository<T>` hook for audit/storage.
- **OpenTelemetry** - built-in traces, histograms, and structured log output via the `FlowRules.Engine` meter and activity source.

---

## Installation

```
dotnet add package FlowRules.Engine
```

---

## Quick start

### 1. Define a policy

```csharp
Policy<OrderRequest> policy = PolicyBuilder<OrderRequest>
    .Create()
    .WithId("order-validation")
    .WithName("Order Validation Policy")
    .WithDescription("Validates incoming order requests.")
    .WithVersion("1.0.0")
    .WithRule(
        id: "R001",
        name: "Amount must be positive",
        source: (request, ct) => ValueTask.FromResult(request.Amount > 0),
        failureMessage: request => $"Amount {request.Amount} is not positive.")
    .WithRule(
        id: "R002",
        name: "Customer must be active",
        source: async (request, ct) =>
        {
            return await CheckCustomerActiveAsync(request.CustomerId, ct);
        })
    .Build();
```

### 2. Register with DI

```csharp
// Single policy
builder.Services.AddFlowRules<OrderRequest>(() => policy);

// Multiple policies across different DTO types via registry
builder.Services.AddFlowRules<OrderRequest>(() => orderPolicy);
builder.Services.AddFlowRules<CustomerRequest>(() => customerPolicy);
builder.Services.AddFlowRulesRegistry();

// Disable FlowRules telemetry if the application should not export it
builder.Services.AddFlowRules<OrderRequest>(() => policy, options =>
{
    options.ExportTelemetry = false;
});
```

### 3. Execute a policy

```csharp
// Via IPolicyManager<T> - single-policy injection
PolicyExecutionResult result = await policyManager.Execute(
    correlationId: "abc-123",
    executionContextId: Guid.NewGuid(),
    request: new OrderRequest { Amount = 100, CustomerId = 42 },
    cancellationToken: ct);

// Via IPolicyRegistry - multi-policy workflows, dispatch by id
PolicyExecutionResult result = await registry.ExecuteAsync<OrderRequest>(
    policyId: "order-validation",
    correlationId: "abc-123",
    executionContextId: Guid.NewGuid(),
    request: new OrderRequest { Amount = 100, CustomerId = 42 },
    cancellationToken: ct);
```

### 4. Inspect the result

```csharp
if (!result.Passed)
{
    foreach (RuleExecutionResult rule in result.RuleExecutionResults.Where(r => !r.Passed))
    {
        Console.WriteLine($"[{rule.Id}] {rule.Message}");
    }
}
```

---

## OpenTelemetry

Register the engine's meter and activity source using the shared constants from `FlowRulesTelemetry`:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t.AddSource(FlowRulesTelemetry.ActivitySourceName))
    .WithMetrics(m => m.AddMeter(FlowRulesTelemetry.MeterName));
```

| Signal             | Name                          |
|--------------------|-------------------------------|
| Traces / Metrics   | `FlowRules.Engine`            |
| Span - policy      | `flowrules.policy.execute`    |
| Span - rule        | `flowrules.rule.execute`      |
| Histogram - policy | `flowrules.policy.duration`   |
| Histogram - rule   | `flowrules.rule.duration`     |

---

## Further information

See the [GitHub repository](https://github.com/p1971/flow.rules) for samples, release notes, and contribution guidelines.
