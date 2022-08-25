using System.Threading.Tasks;

using FlowRules.Engine.Extensions;
using FlowRules.Engine.Interfaces;
using FlowRules.Engine.Models;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Moq;

using Xunit;
using Xunit.Abstractions;

namespace FlowRules.Engine.UnitTests
{
    public class ServiceCollectionExtensionsTests
    {
        private readonly IServiceCollection _subject = new ServiceCollection();

        public ServiceCollectionExtensionsTests(ITestOutputHelper testOutputHelper)
        {
            _subject.AddSingleton<ILogger<PolicyManager<PersonDataModel>>>(
                new TestLogger<PolicyManager<PersonDataModel>>(testOutputHelper));
        }

        [Fact]
        public async Task AddFlowRules_Should_Add_Default_ResultsRepository()
        {
            Policy<PersonDataModel> policy = GetTestPolicy();
            
            _subject.AddFlowRules<PersonDataModel>(() => policy);

            ServiceProvider serviceProvider = _subject.BuildServiceProvider();

            Assert.NotNull(serviceProvider.GetService<Policy<PersonDataModel>>());

            Assert.NotNull(serviceProvider.GetService<IPolicyResultsRepository<PersonDataModel>>());
            Assert.Equal(typeof(DefaultPolicyResultsRepository<PersonDataModel>), serviceProvider.GetService<IPolicyResultsRepository<PersonDataModel>>().GetType());

            Assert.NotNull(serviceProvider.GetService<IPolicyManager<PersonDataModel>>());
        }

        [Fact]
        public async Task AddFlowRules_Should_Add_Custom_ResultsRepository()
        {
            Policy<PersonDataModel> policy = GetTestPolicy();

            Mock<IPolicyResultsRepository<PersonDataModel>> mockResultsRepository = new();

            _subject.AddFlowRules<PersonDataModel>(() => policy, o => o.ResultsRepository = mockResultsRepository.Object.GetType());

            ServiceProvider serviceProvider = _subject.BuildServiceProvider();

            Assert.NotNull(serviceProvider.GetService<Policy<PersonDataModel>>());

            Assert.NotNull(serviceProvider.GetService<IPolicyResultsRepository<PersonDataModel>>());
            Assert.Equal(mockResultsRepository.Object.GetType(), serviceProvider.GetService<IPolicyResultsRepository<PersonDataModel>>().GetType());

            Assert.NotNull(serviceProvider.GetService<IPolicyManager<PersonDataModel>>());
        }

        private static Policy<PersonDataModel> GetTestPolicy()
        {
            Policy<PersonDataModel> policy = PolicyBuilder<PersonDataModel>.Instance
                .WithId("P001")
                .WithName("test policy")
                .WithDescription("policy description")
                .WithRule("R001", "test rule", (model, token) => Task.FromResult(true), description: "test description")
                .Build();
            return policy;
        }
    }
}
