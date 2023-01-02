using System.Threading.Tasks;
using FlowRules.Engine.Models;

namespace FlowRules.Engine.Interfaces
{
    /// <summary>
    /// Interface used to persist the policy metadata and rules.
    /// </summary>
    /// <typeparam name="T">The type the policy was written for.</typeparam>
    public interface IPolicyAuditRepository<T>
        where T : class
    {
        /// <summary>
        /// Persist the policy and metadata to the data store.
        /// </summary>
        /// <param name="policy">The policy to store.</param>
        /// <returns>A task.</returns>
        Task PersistPolicy(Policy<T> policy);
    }
}
