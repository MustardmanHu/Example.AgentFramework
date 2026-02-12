using GenerativeAI.Microsoft;
using Microsoft.Agents.AI;

namespace AgentFrameworkwithGemini.Agents
{
    public class AgentCreator
    {
        private readonly GenerativeAIChatClient _client;
        public AgentCreator(GenerativeAIChatClient client)
        {
            _client = client;
        }

        public ChatClientAgent CreateNewAgent(string name, string instructions)
        {
            return new ChatClientAgent(_client, name: name, instructions: instructions);
        }

    }
}
