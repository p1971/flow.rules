using FlowRules.Engine.Extensions;
using FlowRules.Engine.Interfaces;
using FlowRules.Engine.Models;
#if SQLSERVER
using FlowRules.Extensions.SqlServer;
#endif
using FlowRules.Samples.TestPolicy;
using FlowRules.Samples.WebApi;
using Microsoft.AspNetCore.Mvc;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

#if SQLSERVER
builder.Services
    .AddOptions<SqlServerPolicyResultsRepositoryConfig>()
    .Bind(builder.Configuration.GetSection(nameof(SqlServerPolicyResultsRepositoryConfig)));
builder.Services.AddScoped<IPolicyResultsRepository<MortgageApplication>, SqlServerPolicyResultsRepository<MortgageApplication>>();
#endif

builder.Services.AddFlowRules<MortgageApplication>(PolicySetup.GetPolicy);

builder.AddServiceDefaults();

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

app.MapPost("/_execute", async (
    [FromBody] MortgageApplication mortgageApplication,
    [FromServices] IPolicyManager<MortgageApplication> policyManager,
    [FromHeader(Name = "traceparent")] string? correlationIdHeader,
    CancellationToken cancellationToken) =>
{
    string correlationId = correlationIdHeader ?? Guid.NewGuid().ToString();

    PolicyExecutionResult results = await policyManager.Execute(correlationId, Guid.NewGuid(), mortgageApplication, cancellationToken);

    return Results.Ok(PolicyExecutionResponse.From(results));
});

app.Run();
