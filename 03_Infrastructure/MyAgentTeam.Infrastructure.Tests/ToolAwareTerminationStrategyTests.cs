using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyAgentTeam.Infrastructure.Services;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyAgentTeam.Infrastructure.Tests
{
    [TestClass]
    public class ToolAwareTerminationStrategyTests
    {
        // Helper class to expose the protected method
        private class TestableToolAwareTerminationStrategy : AgentOrchestrator.ToolAwareTerminationStrategy
        {
            public Task<bool> ExecuteShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
            {
                return ShouldAgentTerminateAsync(agent, history, cancellationToken);
            }
        }

        private TestableToolAwareTerminationStrategy _strategy = null!;

        [TestInitialize]
        public void Setup()
        {
            _strategy = new TestableToolAwareTerminationStrategy();
        }

        [TestMethod]
        public async Task ShouldTerminate_WhenContentContainsApproved()
        {
            // Arrange
            var history = new List<ChatMessageContent>
            {
                new ChatMessageContent(AuthorRole.Assistant, "Here is the plan. APPROVED")
            };

            // Act
            var result = await _strategy.ExecuteShouldAgentTerminateAsync(null!, history, CancellationToken.None);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task ShouldTerminate_WhenContentContainsProjectFailed()
        {
            // Arrange
            var history = new List<ChatMessageContent>
            {
                new ChatMessageContent(AuthorRole.Assistant, "Something went wrong. PROJECT_FAILED")
            };

            // Act
            var result = await _strategy.ExecuteShouldAgentTerminateAsync(null!, history, CancellationToken.None);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        [DataRow("file_system.WriteFile(relativePath='test.txt', content='test')")]
        [DataRow("file_system.ReadFile(relativePath='test.txt')")]
        [DataRow("file_system.ListFiles()")]
        [DataRow("shell.RunShellCommand(command='ls')")]
        public async Task ShouldTerminate_WhenContentContainsToolCall(string toolCall)
        {
            // Arrange
            var history = new List<ChatMessageContent>
            {
                new ChatMessageContent(AuthorRole.Assistant, $"I will execute this: {toolCall}")
            };

            // Act
            var result = await _strategy.ExecuteShouldAgentTerminateAsync(null!, history, CancellationToken.None);

            // Assert
            Assert.IsTrue(result, $"Should terminate for tool call: {toolCall}");
        }

        [TestMethod]
        public async Task ShouldNotTerminate_WhenContentIsNormalMessage()
        {
            // Arrange
            var history = new List<ChatMessageContent>
            {
                new ChatMessageContent(AuthorRole.Assistant, "Just a normal conversation message.")
            };

            // Act
            var result = await _strategy.ExecuteShouldAgentTerminateAsync(null!, history, CancellationToken.None);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task ShouldTerminate_WhenContentContainsApproved_CaseInsensitive()
        {
            // Arrange
            var history = new List<ChatMessageContent>
            {
                new ChatMessageContent(AuthorRole.Assistant, "Plan looks good. approved")
            };

            // Act
            var result = await _strategy.ExecuteShouldAgentTerminateAsync(null!, history, CancellationToken.None);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task ShouldTerminate_WhenContentContainsProjectFailed_CaseInsensitive()
        {
            // Arrange
            var history = new List<ChatMessageContent>
            {
                new ChatMessageContent(AuthorRole.Assistant, "project_failed due to errors.")
            };

            // Act
            var result = await _strategy.ExecuteShouldAgentTerminateAsync(null!, history, CancellationToken.None);

            // Assert
            Assert.IsTrue(result);
        }
    }
}
