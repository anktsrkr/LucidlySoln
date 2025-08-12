using Lucidly.UI.McpServer.WeatherAPI.Client;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace Lucidly.UI.McpServer.Client
{
    public class WeatherAPIClientFactory(HttpClient _httpClient, IAuthenticationProvider _authenticationProvider)
    {
        public WeatherAPIClient  GetClient()
        {
            return new WeatherAPIClient(new HttpClientRequestAdapter(_authenticationProvider, httpClient: _httpClient));
        }
    }
}
