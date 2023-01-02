using System.Data;
using System.Reflection;
using System.Transactions;

using Dapper;

using FlowRules.Engine.Interfaces;
using FlowRules.Engine.Models;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace FlowRules.Extensions.SqlServer
{
    /// <inheritdoc />
    public class SqlServerPolicyAuditRepository<T> : IPolicyAuditRepository<T>
        where T : class
    {
        private readonly SqlServerPolicyRepositoryConfig _config;

        private readonly string _sqlInsertPolicy;
        private readonly string _sqlInsertRule;

        private readonly string _policyVersion;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public SqlServerPolicyAuditRepository(IOptions<SqlServerPolicyRepositoryConfig> config)
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

            _policyVersion = Assembly.GetExecutingAssembly().GetName()?.Version?.ToString(4) ?? "0.0.0.0";

            _sqlInsertPolicy = $@"
            DECLARE @existingPolicyId INT
            SELECT @existingPolicyId = Id FROM [{_config.SchemaName}].[Policy] WHERE PolicyId = @PolicyId AND PolicyVersion = @PolicyVersion;

            IF @existingPolicyId  IS NULL
            BEGIN
                INSERT INTO [{_config.SchemaName}].[Policy]
                    (PolicyId, PolicyVersion)
                VALUES 
                    (@PolicyId, @PolicyVersion)
            END

            SELECT ISNULL(@existingPolicyId, SCOPE_IDENTITY())
        ";

            _sqlInsertRule = $@"
            DECLARE @existingRuleId INT
            SELECT @existingRuleId = Id FROM [{_config.SchemaName}].[Rule] WHERE RuleId = @RuleId AND Policy_Id = @Policy_Id;

            IF @existingRuleId IS NULL
            BEGIN
            INSERT INTO [{_config.SchemaName}].[Rule]
                (Policy_Id, RuleId, Name, Source)
            VALUES 
                (@Policy_Id, @RuleId, @Name, @Source)
            END

            SELECT ISNULL(@existingRuleId, SCOPE_IDENTITY())
        ";
        }

        /// <inheritdoc />
        public async Task PersistPolicy(Policy<T> policy)
        {
            using TransactionScope scope = new(TransactionScopeAsyncFlowOption.Enabled);

            using IDbConnection connection = new SqlConnection(_config.ConnectionString);

            connection.Open();

            int policyId = await connection.ExecuteScalarAsync<int>(_sqlInsertPolicy, new
            {
                PolicyId = policy.Id,
                Name = policy.Name,
                Description = policy.Description,
                PolicyVersion = _policyVersion
            });

            foreach (Rule<T>? rule in policy.Rules)
            {
                await connection.ExecuteAsync(_sqlInsertRule,
                    new
                    {
                        Policy_Id = policyId,
                        RuleId = rule.Id,
                        Name = rule.Name,
                        Source = rule.Code
                    });
            }

            scope.Complete();
        }
    }
}
