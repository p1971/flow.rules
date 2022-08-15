﻿using System;
using System.Reflection.PortableExecutable;
using System.Threading;
using System.Threading.Tasks;

using FlowRules.Engine.Interfaces;
using FlowRules.Engine.Models;

using Microsoft.Extensions.Logging;

using Moq;

using Xunit;
using Xunit.Abstractions;

namespace FlowRules.Engine.UnitTests
{
    public class PolicyManagerTests
    {
        private readonly ILogger<PolicyManager<PersonDataModel>> _logger;

        public PolicyManagerTests(ITestOutputHelper testOutputHelper)
        {
            _logger = testOutputHelper.BuildLoggerFor<PolicyManager<PersonDataModel>>();
        }

        [Fact]
        public async Task Execute_Should_Handle_Map_Results()
        {
            Policy<PersonDataModel> policy = PolicyBuilder<PersonDataModel>.Instance
                .WithId("P001")
                .WithName("test policy")
                .WithRule("R001", "test rule", (model, token) => Task.FromResult(true), description: "test description")
                .Build();

            PersonDataModel personDataModel = new("Test User", new DateOnly(2000, 01, 01));

            PolicyExecutionResult response = await ExecutePolicy(policy, personDataModel);

            Assert.NotNull(response);
            Assert.Single(response.RuleExecutionResults);
            Assert.Equal("P001", response.PolicyId);
            Assert.Equal("test policy", response.PolicyName);

            Assert.True(response.RuleExecutionResults[0].Passed);
            Assert.Equal("R001", response.RuleExecutionResults[0].Id);
            Assert.Equal("test rule", response.RuleExecutionResults[0].Name);
            Assert.Equal("test description", response.RuleExecutionResults[0].Description);
        }

        [Fact]
        public async Task Execute_Should_Handle_Exceptions_In_Rules()
        {
            Policy<PersonDataModel> policy = PolicyBuilder<PersonDataModel>.Instance
                .WithId("P001")
                .WithName("test")
                .WithRule("R001", "test", (model, token) => throw new InvalidOperationException())
                .Build();

            PersonDataModel personDataModel = new("Test User", new DateOnly(2000, 01, 01));

            PolicyExecutionResult response = await ExecutePolicy(policy, personDataModel);

            Assert.NotNull(response);
            Assert.Single(response.RuleExecutionResults);
            Assert.False(response.RuleExecutionResults[0].Passed);
        }

        [Fact]
        public async Task Execute_Should_Format_Failure_Message()
        {
            Policy<PersonDataModel> policy = PolicyBuilder<PersonDataModel>.Instance
                .WithId("P001")
                .WithName("test")
                .WithRule("R001", "test", (model, token) => Task.FromResult(false), failureMessage: model => $"Failed for {model.Name}")
                .Build();

            PersonDataModel personDataModel = new("Test User", new DateOnly(2000, 01, 01));

            PolicyExecutionResult response = await ExecutePolicy(policy, personDataModel);

            Assert.NotNull(response);
            Assert.Single(response.RuleExecutionResults);
            Assert.False(response.RuleExecutionResults[0].Passed);
            Assert.Equal($"Failed for {personDataModel.Name}", response.RuleExecutionResults[0].Message);
        }

        [Fact]
        public async Task Execute_Should_Call_All_Rules()
        {
            Policy<PersonDataModel> policy = PolicyBuilder<PersonDataModel>.Instance
                .WithId("P001")
                .WithName("test policy")
                .WithRule("R001", "rule1", (model, token) => Task.FromResult(true), description: "test description 1")
                .WithRule("R002", "rule2", (model, token) => Task.FromResult(true), description: "test description 2")
                .Build();

            PersonDataModel personDataModel = new("Test User", new DateOnly(2000, 01, 01));

            PolicyExecutionResult response = await ExecutePolicy(policy, personDataModel, 100);

            Assert.NotNull(response);
            Assert.Equal(2, response.RuleExecutionResults.Length);
        }

        [Fact]
        public async Task Execute_Should_Allow_Cancellation()
        {
            Policy<PersonDataModel> policy = PolicyBuilder<PersonDataModel>.Instance
                .WithId("P001")
                .WithName("test policy")
                .WithRule("R001", "rule1", async (model, token) =>
                {
                    await Task.Delay(200, token);
                    return true;
                }, description: "test description 1")
                .Build();

            PersonDataModel personDataModel = new("Test User", new DateOnly(2000, 01, 01));

            PolicyExecutionResult response = await ExecutePolicy(policy, personDataModel, 100);

            Assert.NotNull(response);
            Assert.Single(response.RuleExecutionResults);
        }

        [Fact]
        public async Task Execute_Should_NotThrow_OnFailingToPersistResults()
        {
            Policy<PersonDataModel> policy = PolicyBuilder<PersonDataModel>.Instance
                .WithId("P001")
                .WithName("test policy")
                .WithRule("R001", "rule1", (model, token) => Task.FromResult(true), description: "test description 1")
                .Build();

            PersonDataModel personDataModel = new("Test User", new DateOnly(2000, 01, 01));

            Mock<IPolicyResultsRepository<PersonDataModel>> moqPolicyResultsRepository = new();
            moqPolicyResultsRepository.Setup(a =>
                    a.PersistResults(personDataModel, It.IsAny<PolicyExecutionResult>()))
                .Throws<InvalidOperationException>();

            PolicyManager<PersonDataModel> policyManager = new(policy, moqPolicyResultsRepository.Object, _logger);

            CancellationTokenSource cancellationTokenSource = new();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            PolicyExecutionResult response
                = await policyManager.Execute(Guid.NewGuid(), personDataModel, cancellationToken);

            Assert.NotNull(response);
            Assert.Single(response.RuleExecutionResults);
            Assert.True(response.RuleExecutionResults[0].Passed);

        }

        private async Task<PolicyExecutionResult> ExecutePolicy(Policy<PersonDataModel> policy, PersonDataModel personDataModel, int? timeoutInMilliseconds = null)
        {
            PolicyManager<PersonDataModel> policyManager = new(policy, new DefaultPolicyResultsRepository<PersonDataModel>(), _logger);

            CancellationTokenSource cancellationTokenSource =
                timeoutInMilliseconds != null
                    ? new(timeoutInMilliseconds.Value)
                    : new();

            CancellationToken cancellationToken = cancellationTokenSource.Token;

            PolicyExecutionResult response
                = await policyManager.Execute(Guid.NewGuid(), personDataModel, cancellationToken);

            return response;
        }
    }
}
