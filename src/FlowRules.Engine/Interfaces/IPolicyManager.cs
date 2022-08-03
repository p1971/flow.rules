using System;
using System.Threading;
using System.Threading.Tasks;

using FlowRules.Engine.Models;

namespace FlowRules.Engine.Interfaces
{
    public interface IPolicyManager<in T>
        where T : class
    {
        Task<PolicyExecutionResult> Execute(Guid executionContextId, T request, CancellationToken cancellationToken);
    }
}
