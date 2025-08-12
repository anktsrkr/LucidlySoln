using Lucidly.API.Hubs;
using Lucidly.API.Infra;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.SemanticKernel;
using OllamaSharp.Models;
using OpenAI;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
//builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
//    ConnectionMultiplexer.Connect("localhost")); // Use config or env var for production

// Add group stream manager
//builder.Services.AddSingleton<GroupStreamManager>();
builder.Services.AddSingleton<GroupAccessor>();
// Add Redis group listener
//builder.Services.AddSingleton<RedisGroupListener>();
builder.Services.AddSignalR();
builder.Services.AddKernel();

OpenAIClient client = new OpenAIClient(new("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"), new() { Endpoint = new Uri("https://openrouter.ai/api/v1/") });

//builder.Services.AddOpenAIChatCompletion("deepseek/deepseek-chat-v3-0324:free", client);
//builder.Services.AddOpenAIChatCompletion("qwen/qwen3-8b", new Uri("http://localhost:11435/v1"), apiKey: "");
builder.Services.AddOpenAIChatCompletion("qwen/qwen3-coder-30b", new Uri("http://localhost:11435/v1"), apiKey: "");

//builder.Services.AddOpenAIChatCompletion("ibm/granite-3.2-8b", new Uri("http://localhost:11435/v1"), apiKey: "");
//builder.Services.AddOllamaChatCompletion("deepseek-r1:8b", new Uri("http://localhost:11434/"));//hf.co/ibm-granite/granite-3.3-8b-instruct-GGUF:Q5_K_M
//builder.Services.AddOllamaChatCompletion("qwen3:4b", new Uri("http://localhost:11434/"));//hf.co/ibm-granite/granite-3.3-8b-instruct-GGUF:Q5_K_M
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        ["application/octet-stream"]);
});
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<McpClientManager>(); builder.Services.AddSingleton<FunctionApprovalStore>();

var app = builder.Build();

app.UseResponseCompression();

app.UseHttpsRedirection();

app.MapHub<SoloChatHub>("/solo");
var approvals = app.MapGroup("/api/approvals");

approvals.MapGet("/pending", (FunctionApprovalStore store) =>
{
    return Results.Ok(store.GetPending());
});

approvals.MapPost("/approve/{id}", (string id, FunctionApprovalStore store) =>
{
    return store.Approve(id) ? Results.Ok() : Results.NotFound();
});

approvals.MapPost("/reject/{id}", (string id, FunctionApprovalStore store) =>
{
    return store.Reject(id) ? Results.Ok() : Results.NotFound();
});
await app.RunAsync();

