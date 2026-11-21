using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using FlowRules.Engine.Interfaces;
using FlowRules.Engine.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace FlowRules.Extensions.SqlServer;

public class SqlServerPolicyResultsRepository<T> : IPolicyResultsRepository<T>
    where T : class
{
    // Seven parameters per rule plus the shared policy result id stay below
    // SQL Server's 2,100-parameter limit and 1,000-row VALUES limit.
    private const int RuleInsertBatchSize = 250;

    private static readonly Regex SchemaNamePattern = new(
        "^[A-Za-z_][A-Za-z0-9_]{0,127}$",
        RegexOptions.CultureInvariant);

    private readonly SqlServerPolicyResultsRepositoryConfig _config;

    private readonly string _sqlInsertFlowRulesRequest;
    private readonly string _sqlInsertFlowRulesPolicyResult;
    private readonly string _sqlInsertFlowRulesRuleResult;

    public SqlServerPolicyResultsRepository(IOptions<SqlServerPolicyResultsRepositoryConfig> config)
    {
        _config = config.Value;

        if (string.IsNullOrEmpty(_config.ConnectionString))
        {
            throw new InvalidOperationException($"[{nameof(_config.ConnectionString)}] is not set.");
        }

        if (string.IsNullOrWhiteSpace(_config.SchemaName))
        {
            throw new InvalidOperationException($"[{nameof(_config.SchemaName)}] is not set.");
        }

        if (!SchemaNamePattern.IsMatch(_config.SchemaName))
        {
            throw new InvalidOperationException(
                $"[{nameof(_config.SchemaName)}] must be a valid unquoted SQL identifier containing no more than 128 letters, digits, or underscores, and it cannot start with a digit.");
        }
            
        _sqlInsertFlowRulesRequest = @$"
                INSERT INTO [{_config.SchemaName}].[FlowRulesRequest]
                    (FlowExecutionId, CorrelationId, PolicyId, Request)
                VALUES
                    (@FlowExecutionId, @CorrelationId, @PolicyId, @Request)

                SELECT SCOPE_IDENTITY()
            ";

        _sqlInsertFlowRulesPolicyResult = $@"
                INSERT INTO [{_config.SchemaName}].[FlowRulesPolicyResult]
                    (FlowRulesRequest_Id, PolicyName, Passed, Version)  
                VALUES 
                    (@FlowRulesRequest_Id, @PolicyName, @Passed, @Version)  

                SELECT SCOPE_IDENTITY()
            ";

        _sqlInsertFlowRulesRuleResult = $@"
                INSERT INTO [{_config.SchemaName}].[FlowRulesRuleResult]
                    (FlowRulesPolicyResult_Id, RuleId, RuleName, RuleDescription, Passed, Message, Elapsed, Exception)
                VALUES
            ";
    }

    public async ValueTask PersistResults(T request, PolicyExecutionResult policyExecutionResult)
    {
        using TransactionScope scope = new(TransactionScopeAsyncFlowOption.Enabled);

        await using SqlConnection connection = new(_config.ConnectionString);

        await connection.OpenAsync();

        int requestId = await connection.ExecuteScalarAsync<int>(_sqlInsertFlowRulesRequest, new
        {
            FlowExecutionId = policyExecutionResult.RuleContextId,
            CorrelationId = policyExecutionResult.CorrelationId,
            PolicyId = policyExecutionResult.PolicyId,
            Request = JsonSerializer.Serialize(request)
        });

        int policyResultId = await connection.ExecuteScalarAsync<int>(_sqlInsertFlowRulesPolicyResult, new
        {
            FlowRulesRequest_Id = requestId,
            PolicyName = policyExecutionResult.PolicyName,
            Passed = policyExecutionResult.Passed,
            Version = policyExecutionResult.Version
        });

        foreach (RuleExecutionResult[] batch in policyExecutionResult.RuleExecutionResults.Chunk(RuleInsertBatchSize))
        {
            StringBuilder sql = new(_sqlInsertFlowRulesRuleResult);
            DynamicParameters parameters = new();
            parameters.Add("FlowRulesPolicyResult_Id", policyResultId);

            for (int index = 0; index < batch.Length; index++)
            {
                if (index > 0)
                {
                    sql.Append(',');
                }

                sql.Append($"(@FlowRulesPolicyResult_Id, @RuleId{index}, @RuleName{index}, @RuleDescription{index}, @Passed{index}, @Message{index}, @Elapsed{index}, @Exception{index})");

                RuleExecutionResult ruleResult = batch[index];
                parameters.Add($"RuleId{index}", ruleResult.Id);
                parameters.Add($"RuleName{index}", ruleResult.Name);
                parameters.Add($"RuleDescription{index}", ruleResult.Description);
                parameters.Add($"Passed{index}", ruleResult.Passed);
                parameters.Add($"Message{index}", ruleResult.Message);
                parameters.Add($"Elapsed{index}", ruleResult.Elapsed);
                parameters.Add($"Exception{index}", ruleResult.Exception?.Message);
            }

            await connection.ExecuteAsync(sql.ToString(), parameters);
        }

        scope.Complete();
    }
}
