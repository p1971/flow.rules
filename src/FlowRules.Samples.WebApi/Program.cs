using FlowRules.Engine.Extensions;
using FlowRules.Engine.Interfaces;
using FlowRules.Engine.Models;
#if SQLSERVER
using FlowRules.Extensions.SqlServer;
#endif
using FlowRules.Samples.TestPolicy;
using FlowRules.Samples.WebApi;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Trace;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

#if SQLSERVER
builder.Services
    .AddOptions<SqlServerPolicyResultsRepositoryConfig>()
    .Bind(builder.Configuration.GetSection(nameof(SqlServerPolicyResultsRepositoryConfig)));
#endif

builder.Services.AddFlowRules<MortgageApplication>(PolicySetup.GetPolicy, (c) =>
{
#if SQLSERVER
    c.ResultsRepository = typeof(SqlServerPolicyResultsRepository<MortgageApplication>);
#endif
});

builder.AddServiceDefaults();

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

app.MapPost("/_execute", async (
    [FromBody] MortgageApplication mortgageApplication,
    [FromServices] IPolicyManager<MortgageApplication> policyManager,
    [FromHeader(Name = "traceparent")] string? correlationIdHeader,
    [FromServices] TracerProvider tracerProvider,
    CancellationToken cancellationToken) =>
{
    string correlationId = correlationIdHeader ?? Guid.NewGuid().ToString();

    PolicyExecutionResult results = await policyManager.Execute(correlationId, Guid.NewGuid(), mortgageApplication, cancellationToken);

    return results;
});

app.Run();
