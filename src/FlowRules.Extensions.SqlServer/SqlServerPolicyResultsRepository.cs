using System.Data;
using System.Reflection;
using System.Text.Json;
using System.Transactions;

using Dapper;

using FlowRules.Engine.Interfaces;
using FlowRules.Engine.Models;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace FlowRules.Extensions.SqlServer;

/// <inheritdoc />
public class SqlServerPolicyResultsRepository<T> : IPolicyResultsRepository<T>
    where T : class
{
    private readonly SqlServerPolicyRepositoryConfig _config;

    private readonly string _sqlInsertRequest;
    private readonly string _sqlInsertPolicyResult;
    private readonly string _sqlInsertRuleResult;
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="config"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public SqlServerPolicyResultsRepository(IOptions<SqlServerPolicyRepositoryConfig> config)
    {
        _config = config.Value;

        if (string.IsNullOrEmpty(_config.ConnectionString))
        {
            throw new InvalidOperationException($"[{nameof(_config.ConnectionString)}] is not set.");
        }

        if (string.IsNullOrEmpty(_config.SchemaName))
        {
            throw new InvalidOperationException($"[{nameof(_config.SchemaName)}] is not set.");
        }

        _sqlInsertRequest = @$"
                INSERT INTO [{_config.SchemaName}].[Request]
                    (FlowExecutionId, CorrelationId, PolicyId, Request)
                VALUES
                    (@FlowExecutionId, @CorrelationId, @PolicyId, @Request)

                SELECT SCOPE_IDENTITY()
            ";

        _sqlInsertPolicyResult = $@"
                INSERT INTO [{_config.SchemaName}].[PolicyResult]
                    (Request_Id, PolicyName, Passed, Version)  
                VALUES 
                    (@Request_Id, @PolicyName, @Passed, @Version)  

                SELECT SCOPE_IDENTITY()
            ";

        _sqlInsertRuleResult = $@"
                INSERT INTO [{_config.SchemaName}].[RuleResult]
                    (PolicyResult_Id, RuleId, RuleName, RuleDescription, Passed, Message, Elapsed, Exception)
                VALUES
                    (@PolicyResult_Id, @RuleId, @RuleName, @RuleDescription, @Passed, @Message, @Elapsed, @Exception)
            ";
    }

    /// <inheritdoc />
    public async Task PersistResults(T request, PolicyExecutionResult policyExecutionResult)
    {
        using TransactionScope scope = new(TransactionScopeAsyncFlowOption.Enabled);

        using IDbConnection connection = new SqlConnection(_config.ConnectionString);

        connection.Open();

        int requestId = await connection.ExecuteScalarAsync<int>(_sqlInsertRequest, new
        {
            FlowExecutionId = policyExecutionResult.RuleContextId,
            CorrelationId = policyExecutionResult.CorrelationId,
            PolicyId = policyExecutionResult.PolicyId,
            Request = JsonSerializer.Serialize(request)
        });

        int policyResultId = await connection.ExecuteScalarAsync<int>(_sqlInsertPolicyResult, new
        {
            Request_Id = requestId,
            PolicyName = policyExecutionResult.PolicyName,
            Passed = policyExecutionResult.Passed,
            Version = policyExecutionResult.Version
        });

        foreach (RuleExecutionResult? ruleResult in policyExecutionResult.RuleExecutionResults)
        {
            await connection.ExecuteAsync(_sqlInsertRuleResult, new
            {
                PolicyResult_Id = policyResultId,
                RuleId = ruleResult.Id,
                RuleName = ruleResult.Name,
                RuleDescription = ruleResult.Description,
                Passed = ruleResult.Passed,
                Message = ruleResult.Message,
                Elapsed = ruleResult.Elapsed,
                Exception = ruleResult.Exception?.Message
            });
        }

        scope.Complete();
    }
}
