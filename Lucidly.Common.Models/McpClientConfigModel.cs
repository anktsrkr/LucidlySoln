using ModelContextProtocol.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucidly.Common.Models
{
    public class AdditionalParameters
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
    public class McpClientConfigModel
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string Url { get; set; }
        [Required]
        public MCPTransportMode MCPTransportMode { get; set; } = MCPTransportMode.Sse;

        [Required]
        public McpClientRegistrationMode McpClientRegistrationMode { get; set; } = McpClientRegistrationMode.Dynamic;

        [Required]
        public McpClientAuthenticationMode McpClientAuthenticationMode { get; set; } = McpClientAuthenticationMode.NoAuth;

        public List<AdditionalParameters> AdditionalAuthorizationParameters { get; set; } = [];
        public List<McpClientTool> Tools { get; set; } = [];
        public List<McpClientPrompt> Prompts { get; set; } = [];
        public string? AccessToken { get; set; } = string.Empty;


        [Required] public string ClientId { get; set; }
        [Required] public string ClientSecrect { get; set; }

        [Required] public string ApiKeyName { get; set; }
        [Required] public string ApiKeyValue { get; set; }

    }



    public class McpServerToSend
    {
        public string ServerName { get; set; }
        public string Uri { get; set; }
        public MCPTransportMode TransportMode { get; set; }
        public Dictionary<string, string> AdditionalHeaders { get; set; }
        public List<McpToolToSend> ToolsToSend { get; set; }
        public List<McpPromptToSend> PromptsToSend { get; set; }
    }
    public class McpToolToSend
    {
        public string ToolName { get; set; }
    }
    public class McpPromptToSend
    {
        public string PromptName { get; set; }
        public Dictionary<string, object> PromptArgument { get; set; }
    }

}
