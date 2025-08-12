using Lucidly.Common;
using Lucidly.Common.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Caching.Memory;
using ModelContextProtocol.Client;

namespace Lucidly.UI.Utils
{
    public class McpClientManager(IMemoryCache memoryCache, NavigationManager Navigation, OAuthHandler OAuthHandler)
    {
        private readonly MemoryCacheEntryOptions _cacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(30) // Auto-evict if not used for 30 minutes
        };
        public async Task<IMcpClient> GetOrCreateClientAsync(string serverUrl, McpClientConfigModel config)
        {
            if (memoryCache.TryGetValue(serverUrl, out IMcpClient? client))
            {
                return client!;
            }

            var sseClientTransportOptions = new SseClientTransport(new SseClientTransportOptions
            {
                Endpoint = new(config.Url),
                AdditionalHeaders = config.McpClientAuthenticationMode == McpClientAuthenticationMode.ApiKey ? new Dictionary<string, string> { { config.ApiKeyName, config.ApiKeyValue } } : null,
                TransportMode = config.MCPTransportMode == MCPTransportMode.Sse ? HttpTransportMode.Sse : HttpTransportMode.StreamableHttp,
                OAuth = config.McpClientAuthenticationMode == McpClientAuthenticationMode.OAuth ? new()
                {
                    ClientId = config.ClientId,
                    ClientSecret = config.ClientSecrect,
                    ClientName = "lucidly",
                    RedirectUri = new Uri(Navigation.BaseUri + "callback"),
                    AuthorizationRedirectDelegate = OAuthHandler.HandleAuthorizationUrlAsync,
                    AdditionalAuthorizationParameters = config.AdditionalAuthorizationParameters.ToDictionary(x => x.Key, x => x.Value)
                } : null
            });

            client =   await McpClientFactory.CreateAsync(sseClientTransportOptions);
          //  sseClientTransportOptions.CurrentAccessToken
            memoryCache.Set(serverUrl, client, _cacheOptions);

            return client;

        }
    }
}
