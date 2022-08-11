namespace FlowRules.Extensions.SqlServer;

public class SqlServerPolicyResultsRepositoryConfig
{
    public string? ConnectionString { get; set; }

    public string SchemaName { get; set; } = "flowrules";
}
