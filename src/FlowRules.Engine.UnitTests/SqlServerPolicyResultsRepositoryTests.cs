using System;

using FlowRules.Extensions.SqlServer;

using Microsoft.Extensions.Options;

using Xunit;

namespace FlowRules.Engine.UnitTests;

public class SqlServerPolicyResultsRepositoryTests
{
    [Theory]
    [InlineData("flowrules")]
    [InlineData("_flow_rules_2")]
    [InlineData("Schema123")]
    public void Constructor_Should_Accept_Valid_Schema_Name(string schemaName)
    {
        SqlServerPolicyResultsRepositoryConfig config = new()
        {
            ConnectionString = "Server=localhost;Database=flowrules;Integrated Security=true",
            SchemaName = schemaName
        };

        SqlServerPolicyResultsRepository<object> repository = new(Options.Create(config));

        Assert.NotNull(repository);
    }

    [Theory]
    [InlineData("1flowrules")]
    [InlineData("flow rules")]
    [InlineData("flowrules].[FlowRulesRequest]; DROP TABLE Audit;--")]
    [InlineData("flowrules]")]
    public void Constructor_Should_Reject_Invalid_Schema_Name(string schemaName)
    {
        SqlServerPolicyResultsRepositoryConfig config = new()
        {
            ConnectionString = "Server=localhost;Database=flowrules;Integrated Security=true",
            SchemaName = schemaName
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => new SqlServerPolicyResultsRepository<object>(Options.Create(config)));

        Assert.Contains(nameof(config.SchemaName), exception.Message, StringComparison.Ordinal);
    }
}
