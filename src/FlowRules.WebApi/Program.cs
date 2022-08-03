using FlowRules.Engine.Extensions;
using FlowRules.Engine.Interfaces;
using FlowRules.Engine.Models;
using FlowRules.Extensions.SqlServer;
using FlowRules.Samples.TestPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton(Options.Create(new SqlServerPolicyResultsRepositoryConfig
{
    ConnectionString = " Server=sharon;Initial Catalog=flowengine;User Id=sa;Password=lnSjN7INewyShvT;TrustServerCertificate=True"
}));

builder.Services.AddFlowRules<MortgageApplication>(PolicySetup.GetPolicy, (c) =>
{
    c.ResultsRepository = typeof(SqlServerPolicyResultsRepository<MortgageApplication>);
});

WebApplication app = builder.Build();

app.MapPost("/_execute", async (
    [FromBody] MortgageApplication mortgageApplication,
    [FromServices] IPolicyManager<MortgageApplication> policyManager,
    CancellationToken cancellationToken) =>
{
    PolicyExecutionResult? result = await policyManager.Execute(Guid.NewGuid(), mortgageApplication, cancellationToken);
    return result;
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();
