using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.Google;
using MyAgentTeam.Infrastructure.Agents;

namespace MyAgentTeam.Infrastructure.Tests;

[TestClass]
public class AgentDefinitionsTests
{
	private Kernel _kernel;

	[TestInitialize]
	public void Initialize()
	{
		_kernel = Kernel.CreateBuilder().Build();
	}

	[TestMethod]
	public void CreateSupervisor_ShouldReturnAgentWithCorrectNameAndInstructions()
	{
		// Arrange
		string sharedInstructions = "Shared instructions";
		bool isNewProject = false;

		// Act
		var agent = AgentDefinitions.CreateSupervisor(_kernel, sharedInstructions, isNewProject);

		// Assert
		Assert.AreEqual("Supervisor", agent.Name);
		Assert.IsTrue(agent.Instructions!.Contains("你是既有專案的維護經理 (Supervisor)"));
		VerifySharedInstructions(agent, "Supervisor", sharedInstructions);
	}

	[TestMethod]
	public void CreateDesigner_ShouldReturnAgentWithCorrectNameAndInstructions()
	{
		// Arrange
		string sharedInstructions = "Shared instructions";
		bool isNewProject = false;

		// Act
		var agent = AgentDefinitions.CreateDesigner(_kernel, sharedInstructions, isNewProject);

		// Assert
		Assert.AreEqual("System_Designer", agent.Name);
		Assert.IsTrue(agent.Instructions!.Contains("你是架構規劃師 (Architect)"));
		VerifySharedInstructions(agent, "System_Designer", sharedInstructions);
	}

	[TestMethod]
	public void CreateProgrammer_ShouldReturnAgentWithCorrectNameAndInstructions()
	{
		// Arrange
		string sharedInstructions = "Shared instructions";
		bool isNewProject = false;

		// Act
		var agent = AgentDefinitions.CreateProgrammer(_kernel, sharedInstructions, isNewProject);

		// Assert
		Assert.AreEqual("Programmer", agent.Name);
		Assert.IsTrue(agent.Instructions!.Contains("你是資深開發者 (Programmer)"));
		VerifySharedInstructions(agent, "Programmer", sharedInstructions);
	}

	[TestMethod]
	public void CreateDBA_ShouldReturnAgentWithCorrectNameAndInstructions()
	{
		// Arrange
		string sharedInstructions = "Shared instructions";
		bool isNewProject = false;

		// Act
		var agent = AgentDefinitions.CreateDBA(_kernel, sharedInstructions, isNewProject);

		// Assert
		Assert.AreEqual("DBA", agent.Name);
		Assert.IsTrue(agent.Instructions!.Contains("你是資料庫管理員 (DBA)"));
		VerifySharedInstructions(agent, "DBA", sharedInstructions);
	}

	[TestMethod]
	public void CreateProgrammerSecond_ShouldReturnAgentWithCorrectNameAndInstructions()
	{
		// Arrange
		string sharedInstructions = "Shared instructions";
		bool isNewProject = false;

		// Act
		var agent = AgentDefinitions.CreateProgrammerSecond(_kernel, sharedInstructions, isNewProject);

		// Assert
		Assert.AreEqual("Second_Programmer", agent.Name);
		Assert.IsTrue(agent.Instructions!.Contains("你是資深除錯專家 (Debugger / Second Programmer)"));
		VerifySharedInstructions(agent, "Second_Programmer", sharedInstructions);
	}

	[TestMethod]
	public void CreateResearcher_ShouldReturnAgentWithCorrectNameAndInstructions()
	{
		// Arrange
		string sharedInstructions = "Shared instructions";
		bool isNewProject = false;

		// Act
		var agent = AgentDefinitions.CreateResearcher(_kernel, sharedInstructions, isNewProject);

		// Assert
		Assert.AreEqual("Researcher", agent.Name);
		Assert.IsTrue(agent.Instructions!.Contains("技術顧問"));
		VerifySharedInstructions(agent, "Researcher", sharedInstructions);
	}

	[TestMethod]
	public void CreateTester_ShouldReturnAgentWithCorrectNameAndInstructions()
	{
		// Arrange
		string sharedInstructions = "Shared instructions";
		bool isNewProject = false;

		// Act
		var agent = AgentDefinitions.CreateTester(_kernel, sharedInstructions, isNewProject);

		// Assert
		Assert.AreEqual("Tester", agent.Name);
		Assert.IsTrue(agent.Instructions!.Contains("你是測試工程師 (Tester)"));
		VerifySharedInstructions(agent, "Tester", sharedInstructions);
	}

	[TestMethod]
	public void CreateQA_ShouldReturnAgentWithCorrectNameAndInstructions()
	{
		// Arrange
		string sharedInstructions = "Shared instructions";
		bool isNewProject = false;

		// Act
		var agent = AgentDefinitions.CreateQA(_kernel, sharedInstructions, isNewProject);

		// Assert
		Assert.AreEqual("QA", agent.Name);
		Assert.IsTrue(agent.Instructions!.Contains("你是品質保證專家 (QA)"));
		VerifySharedInstructions(agent, "QA", sharedInstructions);
	}

	private void VerifySharedInstructions(ChatCompletionAgent agent, string agentName, string sharedInstructions)
	{
		Assert.IsTrue(agent.Instructions!.Contains("【專案最高指導原則 (SUPREME LAW)】"));
		Assert.IsTrue(agent.Instructions!.Contains(sharedInstructions));
		Assert.IsTrue(agent.Instructions!.Contains("【絕對身份認同 (ABSOLUTE IDENTITY PROTOCOL)】"));
		Assert.IsTrue(agent.Instructions!.Contains($"你的名字是：{agentName}"));
		Assert.IsTrue(agent.Instructions!.Contains("【精簡思考協議 (CONCISE THOUGHT PROTOCOL)】"));
		Assert.IsTrue(agent.Instructions!.Contains("【行動強制令 (ACTION MANDATE)】"));
		Assert.IsTrue(agent.Instructions!.Contains("【溝通與工具使用規範】"));
		Assert.IsTrue(agent.Instructions!.Contains("【交接信號協議 (Handoff Protocol)】"));
		Assert.IsTrue(agent.Instructions!.Contains($"REMINDER: YOU ARE {agentName}. DO NOT SIMULATE TOOL RESULTS. STOP AFTER TOOL CALL."));

		Assert.IsNotNull(agent.Arguments);
		Assert.IsNotNull(agent.Kernel);

		var settings = agent.Arguments.ExecutionSettings?.Values.FirstOrDefault() as GeminiPromptExecutionSettings;
		Assert.IsNotNull(settings);
		Assert.AreEqual(0.5, settings.Temperature);
	}
}
