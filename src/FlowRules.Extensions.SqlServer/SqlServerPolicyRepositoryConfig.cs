namespace FlowRules.Extensions.SqlServer;

/// <summary>
/// SQL server database configuration.
/// </summary>
public class SqlServerPolicyRepositoryConfig
{
    /// <summary>
    /// Gets or sets the database connection string.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the database schema name.
    /// <remarks>Defaults to 'flowrules'.</remarks>
    /// </summary>
    public string SchemaName { get; set; } = "flowrules";
}
