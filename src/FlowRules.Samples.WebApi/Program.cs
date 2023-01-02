using FlowRules.Engine.Extensions;
using FlowRules.Engine.Interfaces;
using FlowRules.Extensions.SqlServer;
using FlowRules.Samples.TestPolicy;

using Microsoft.AspNetCore.Mvc;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

#if SQLSERVER
builder.Services
    .AddOptions<SqlServerPolicyRepositoryConfig>()
    .Bind(builder.Configuration.GetSection(nameof(SqlServerPolicyRepositoryConfig)));
#endif

builder.Services.AddFlowRules<MortgageApplication>(PolicySetup.GetPolicy, (c) =>
{
#if SQLSERVER
    c.ResultsRepository = typeof(SqlServerPolicyResultsRepository<MortgageApplication>);
#endif
});

WebApplication app = builder.Build();

app.MapPost("/_execute", async (
    [FromBody] MortgageApplication mortgageApplication,
    [FromServices] IPolicyManager<MortgageApplication> policyManager,
    [FromHeader(Name = "X-Correlation-Id")] string? correlationIdHeader,
    CancellationToken cancellationToken) =>
{
    string correlationId = correlationIdHeader ?? Guid.NewGuid().ToString();
    return await policyManager.Execute(correlationId, Guid.NewGuid(), mortgageApplication, cancellationToken);
});

app.Run();
