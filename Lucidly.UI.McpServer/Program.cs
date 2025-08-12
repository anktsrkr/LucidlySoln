

using Lucidly.UI.McpServer.Client;
using Lucidly.UI.McpServer.Extensions;
using Lucidly.UI.McpServer.WeatherAPI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Kiota.Abstractions.Authentication;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Protocol;
using System.Security.Claims;

var serverUrl = "https://localhost:7046";///sse";
var inMemoryOAuthServerUrl = "https://genai-9788788761862988.uk.auth0.com/";
var demoClientId = "NZs0WUTwKSqwLzSEGJu8SW3s5a5N5dAs";


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(options =>
{
    options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Configure to validate tokens from our in-memory OAuth server
    options.Authority = inMemoryOAuthServerUrl;
    options.Audience = "urn:mcp-weather";
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = inMemoryOAuthServerUrl,
        ClockSkew = TimeSpan.Zero,
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            var name = context.Principal?.Identity?.Name ?? "unknown";
            var email = context.Principal?.FindFirstValue("preferred_username") ?? "unknown";
            Console.WriteLine($"Token validated for: {name} ({email})");
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Console.WriteLine($"Challenging client to authenticate with Entra ID");
            return Task.CompletedTask;
        }
    };
})
.AddMcp(options =>
{
    options.ResourceMetadata = new()
    {

        Resource = new Uri(serverUrl),
        BearerMethodsSupported = { "header" },
        AuthorizationServers = { new Uri(inMemoryOAuthServerUrl) },
        ScopesSupported = ["openid", "email", "profile", "offline_access", "read:todos"],
    };
});


builder.Services.AddKiotaHandlers();
builder.Services.AddSingleton<IAuthenticationProvider>(sp =>
{
    return new ApiKeyAuthenticationProvider("455160cdbf56416bb5254241252307", "key", ApiKeyAuthenticationProvider.KeyLocation.QueryParameter);
});
builder.Services.AddHttpClient<WeatherAPIClientFactory>((sp, client) =>
{
    client.BaseAddress = new Uri("https://api.weatherapi.com/v1");
}).AttachKiotaHandlers(); // Attach the Kiota handlers to the http client, this is to enable all the Kiota features.

builder.Services.AddTransient(sp => sp.GetRequiredService<WeatherAPIClientFactory>().GetClient());


builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthorization();




builder.Logging.AddConsole(consoleLogOptions =>
{
    // Configure all logs to go to stderr
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

HashSet<string> subscriptions = [];
builder.Services.AddMcpServer()
.WithHttpTransport(x => x.Stateless = false)
.WithToolsFromAssembly()
.WithPromptsFromAssembly()
.WithCompleteHandler(async (ctx, ct) =>
{
    var weatherClient = ctx.Services.GetRequiredService<WeatherAPIClient>();


    var exampleCompletions = new Dictionary<string, IEnumerable<string>>
        {
            { "style", ["casual", "formal", "technical", "friendly"] },
            { "temperature", ["0", "0.5", "0.7", "1.0"] },
            { "resourceId", ["1", "2", "3", "4", "5"] }
        };


    if (ctx.Params is not { } @params)
    {
        throw new NotSupportedException($"Params are required.");
    }

    var @ref = @params.Ref;
    var argument = @params.Argument;

    if (@ref is ResourceTemplateReference rtr)
    {
        var resourceId = rtr.Uri?.Split("/").Last();

        if (resourceId is null)
        {
            return new CompleteResult();
        }

        var values = exampleCompletions["resourceId"].Where(id => id.StartsWith(argument.Value));

        return new CompleteResult
        {
            Completion = new Completion { Values = [.. values], HasMore = false, Total = values.Count() }
        };
    }

    if (@ref is PromptReference pr)
    {
        if (argument.Name == "location")
        {
            var locations = await weatherClient.SearchJson.GetAsync(z =>
            {
                z.QueryParameters.Q = argument.Value;
            }, ct);

            if (locations is null || locations.Count == 0)
            {
                return new CompleteResult();
            }
            var locationNames = locations.Select(location => $"{location.Name} (Country: {location.Country})" ); ;
            return new CompleteResult
            {
                Completion = new Completion { Values = [.. locationNames], HasMore = false, Total = locationNames.Count() }
            };
        }
        if (!exampleCompletions.TryGetValue(argument.Name, out IEnumerable<string>? value))
        {
            throw new NotSupportedException($"Unknown argument name: {argument.Name}");
        }

        var values = value.Where(value => value.StartsWith(argument.Value));
        return new CompleteResult
        {
            Completion = new Completion { Values = [.. values], HasMore = false, Total = values.Count() }
        };
    }

    throw new NotSupportedException($"Unknown reference type: {@ref.Type}");
});

// Configure the HTTP request pipeline.

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Use the default MCP policy name that we've configured
app.MapMcp().RequireAuthorization();
await app.RunAsync();
