using Lucidly.Common;
using Lucidly.Common.Models;
using Microsoft.Extensions.Caching.Memory;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace Lucidly.API.Infra
{
    public class McpClientManager(IMemoryCache memoryCache)
    {
        private readonly MemoryCacheEntryOptions _cacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(30) // Auto-evict if not used for 30 minutes
        };

        // Create or get a cached client for a given server
        public async Task<IMcpClient> GetOrCreateClientAsync(string serverUrl, McpServerToSend config, ChannelWriter<ChatBubble> writer, ChatBubble chatBubble, StringBuilder totalCompletion)
        {
            if (memoryCache.TryGetValue(serverUrl, out IMcpClient? client))
            {
                return client!;
            }
            var sseClientTransportOptions = new SseClientTransport(new SseClientTransportOptions
            {
                Endpoint = new(config.Uri),
                AdditionalHeaders = config.AdditionalHeaders,
                TransportMode = config.TransportMode == MCPTransportMode.Sse ? HttpTransportMode.Sse : HttpTransportMode.StreamableHttp,
            });

            client =  await McpClientFactory.CreateAsync(sseClientTransportOptions, new McpClientOptions
            {
                Capabilities = new ClientCapabilities
                {
                    Elicitation = new ElicitationCapability
                    {
                        ElicitationHandler = async (xx, cancellationToken) =>
                        {
                            var schemaDict = new Dictionary<string, PropertySchema>();

                            if (xx.RequestedSchema?.Properties != null)
                            {
                                foreach (var kvp in xx.RequestedSchema.Properties)
                                {
                                    var key = kvp.Key;
                                    var value = kvp.Value;

                                    PropertySchema schema;

                                    switch (value)
                                    {
                                        case ElicitRequestParams.StringSchema s:
                                            schema = new StringPropertySchema
                                            {
                                                Title = s.Title,
                                                Description = s.Description,
                                                MinLength = s.MinLength,
                                                MaxLength = s.MaxLength
                                            };
                                            break;

                                        case ElicitRequestParams.NumberSchema n:
                                            schema = new NumberPropertySchema
                                            {
                                                Title = n.Title,
                                                Description = n.Description,
                                                Minimum = n.Minimum,
                                                Maximum = n.Maximum
                                            };
                                            break;

                                        case ElicitRequestParams.BooleanSchema b:
                                            schema = new BooleanPropertySchema
                                            {
                                                Title = b.Title,
                                                Description = b.Description,
                                                Default = b.Default
                                            };
                                            break;

                                        case ElicitRequestParams.EnumSchema e:
                                            schema = new EnumPropertySchema
                                            {
                                                Title = e.Title,
                                                Description = e.Description,
                                                Enum = e.Enum?.ToList(),
                                                EnumNames = e.EnumNames?.ToList()
                                            };
                                            break;

                                        default:
                                            schema = new StringPropertySchema
                                            {
                                                Type = value.Type,
                                                Title = value.Title,
                                                Description = value.Description
                                            };
                                            break;
                                    }

                                    schemaDict[key] = schema;
                                }
                            }

                            var toolUpdate = new ToolDetails
                            {
                                PluginName = config.ServerName,
                                McpFunctionArgs = schemaDict,
                                Type = "mcp-request"
                            };
                            totalCompletion.Append($"<mcp_tool_update>{JsonSerializer.Serialize(toolUpdate, new JsonSerializerOptions
                            {
                                WriteIndented = true,
                                Converters = { new PropertySchemaConverter() }
                            })}</mcp_tool_update>");

                            chatBubble = chatBubble with
                            {
                                Message = totalCompletion.ToString(),
                                Type = "mcp-request",
                            };

                            await writer.WriteAsync(chatBubble);
                            await Task.Delay(1000 * 60);
                            // Handle elicitation requests here
                            // This is a placeholder; actual implementation will depend on your requirements
                            return new ModelContextProtocol.Protocol.ElicitResult
                            {
                              Action = "cancel",
                              
                            };
                        }
                    },
                },
            });
            memoryCache.Set(serverUrl, client, _cacheOptions);

            return client;

        }

        // Get or fetch tools for a server
        public async Task<IList<McpClientTool>> GetOrFetchToolsAsync(string serverUrl, McpServerToSend config, ChannelWriter<ChatBubble> writer, ChatBubble chatBubble, StringBuilder totalCompletion)
        {
            string toolsKey = $"tools::{serverUrl}";
            if (memoryCache.TryGetValue(toolsKey, out IList<McpClientTool>? tools))
            {
                return tools!;
            }

            var client = await GetOrCreateClientAsync(serverUrl, config, writer, chatBubble, totalCompletion);
            tools = await client.ListToolsAsync();
            memoryCache.Set(toolsKey, tools, _cacheOptions);

            return tools;
        }

        // Get or fetch prompts for a server
        public async Task<IList<McpClientPrompt>> GetOrFetchPromptsAsync(string serverUrl, McpServerToSend config, ChannelWriter<ChatBubble> writer, ChatBubble chatBubble, StringBuilder totalCompletion)
        {
            string promptsKey = $"prompts::{serverUrl}";
            if (memoryCache.TryGetValue(promptsKey, out IList<McpClientPrompt>? prompts))
            {
                return prompts!;
            }

            var client = await GetOrCreateClientAsync(serverUrl, config, writer, chatBubble, totalCompletion);
            if (client .ServerCapabilities.Prompts== null)
            {
                return [];
            }
            prompts = await client.ListPromptsAsync();
            memoryCache.Set(promptsKey, prompts, _cacheOptions);

            return prompts;
        }

        /// <summary>
        /// Get all tools from all provided servers.
        /// </summary>
        /// <param name="serverConfigs">Dictionary of serverUrl => McpClientConfig</param>
        /// <returns>List of all tools across all servers. Each tool's ServerUrl property will be set.</returns>
        public async Task<IList<McpClientTool>> GetAllToolsAsync(List<McpServerToSend> serverConfigs, ChannelWriter<ChatBubble> writer, ChatBubble chatBubble, StringBuilder totalCompletion)
        {
            var allTools = new List<McpClientTool>();
            var tasks = serverConfigs.Select(async kvp => {
                var tools = await GetOrFetchToolsAsync(kvp.Uri, kvp,writer, chatBubble, totalCompletion);
                return tools;
            });

            var results = await Task.WhenAll(tasks);
            foreach (var toolList in results)
            {
                allTools.AddRange(toolList);
            }
            return allTools;
        }

        /// <summary>
        /// Get all tools from all provided servers. if user specified some tools to send, only return those tools.
        /// </summary>
        /// <param name="serverConfigs">Dictionary of serverUrl => McpClientConfig</param>
        /// <returns>List of all tools across all servers. Each tool's ServerUrl property will be set.</returns>
        public async Task<IList<McpClientTool>> GetSelectedToolsAsync(List<McpServerToSend> serverConfigs, ChannelWriter<ChatBubble> writer, ChatBubble chatBubble, StringBuilder totalCompletion)
        {
            var allTools = await GetAllToolsAsync(serverConfigs, writer, chatBubble, totalCompletion);
            var selectedTools = allTools.Where(tool => serverConfigs.Any(c => c.ToolsToSend.Any(ct => ct.ToolName == tool.Name)));
            if (selectedTools.Any())
            {
                return [.. selectedTools];
            }else
            {
                return [];//[.. allTools];
            }
        }
        /// <summary>
        /// Get all Prompts from all provided servers.
        /// </summary>
        /// <param name="serverConfigs">Dictionary of serverUrl => McpClientConfig</param>
        /// <returns>List of all tools across all servers. Each prompt's ServerUrl property will be set.</returns>
        public async Task<IList<McpClientPrompt>> GetAllPromptsAsync(List<McpServerToSend> serverConfigs, ChannelWriter<ChatBubble> writer, ChatBubble chatBubble, StringBuilder totalCompletion)
        {
            var allPrompts = new List<McpClientPrompt>();
            var tasks = serverConfigs.Select(async kvp => {
                var tools = await GetOrFetchPromptsAsync(kvp.Uri, kvp, writer, chatBubble, totalCompletion);
                return tools;
            });

            var results = await Task.WhenAll(tasks);
            foreach (var toolList in results)
            {
                allPrompts.AddRange(toolList);
            }
            return allPrompts;
        }

        /// <summary>
        /// Get all Prompts from all provided servers.
        /// </summary>
        /// <param name="serverConfigs">Dictionary of serverUrl => McpClientConfig</param>
        /// <returns>List of all tools across all servers. Each prompt's ServerUrl property will be set.</returns>
        public async Task<IList<McpClientPrompt>> GetSelectedPromptAsync(List<McpServerToSend> serverConfigs, ChannelWriter<ChatBubble> writer, ChatBubble chatBubble, StringBuilder totalCompletion)
        {
            var allPrompts = new List<McpClientPrompt>();
            var tasks = serverConfigs.Select(async kvp => {
                var tools = await GetOrFetchPromptsAsync(kvp.Uri, kvp, writer, chatBubble, totalCompletion);
                return tools;
            });

            var results = await Task.WhenAll(tasks);
            foreach (var toolList in results)
            {
                allPrompts.AddRange(toolList);
            }
       
            return [.. allPrompts.Where(p => serverConfigs.Any(c => c.PromptsToSend.Any(cp => cp.PromptName == p.Name)))];
        }
    }
}
