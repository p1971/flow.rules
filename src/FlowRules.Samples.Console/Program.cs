using System.Threading.Tasks;

using FlowRules.Engine.Extensions;
using FlowRules.Engine.Interfaces;
using FlowRules.Engine.Models;
using FlowRules.Extensions.SqlServer;
using FlowRules.Samples.TestPolicy;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FlowRules.Samples.Console;

public static class Program
{
    public static async Task Main(string[] args)
    {
        Policy<MortgageApplication> policy = PolicySetup.GetPolicy();

        using IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddLogging(opt => { opt.AddConsole(); });

#if SQLSERVER
                services
                    .AddOptions<SqlServerPolicyRepositoryConfig>()
                    .BindConfiguration(nameof(SqlServerPolicyRepositoryConfig));
#endif

                services.AddFlowRules<MortgageApplication>(() => policy, c =>
                {
#if SQLSERVER
                    c.ResultsRepository = typeof(SqlServerPolicyResultsRepository<MortgageApplication>);
                    c.PolicyAuditRepository = typeof(SqlServerPolicyAuditRepository<MortgageApplication>);
#endif
                });

                services.AddSingleton<RulesService>();

            })
            .Build();

        IPolicyAuditRepository<MortgageApplication> policyAuditRepository =
            host.Services.GetService<IPolicyAuditRepository<MortgageApplication>>();

        if (policyAuditRepository != null)
        {
            await policyAuditRepository.PersistPolicy(policy);
        }

        RulesService rulesService = host.Services.GetRequiredService<RulesService>();

        await rulesService.ExecuteAsync();
    }
}
