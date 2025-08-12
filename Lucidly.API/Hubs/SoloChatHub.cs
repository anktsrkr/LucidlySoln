using Lucidly.API.Infra;
using Lucidly.Common.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Memory;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Text;
using System.Threading;
using System.Threading.Channels;
namespace Lucidly.API.Hubs;

public class SoloChatHub(Kernel kernel, McpClientManager mcpClientManager, GroupAccessor groupAccessor, FunctionApprovalStore functionApprovalStore,IChatCompletionService chatCompletionService) : Hub
{
    //, GroupStreamManager manager, RedisGroupListener redis
    private const string JokerName = "Assistant";
    private const string JokerInstructions = "You are helpful assistant.Have access of multiple tools." +
        "Use tools to respond. " + "Do not Manipulate user's input while passing as tool's argument." +
        "Example: If input has `@xxx messgae`, xxx is the tool name, which you must invoke without manipulating `message` without toolname."+
        "Another Example: If input has `@xxx yy:zz`, xxx is the tool name,yy is the argument name for the tool, zz is the argument value, which you must invoke without manipulating `zz`.";

 static   ChatHistoryAgentThread agentThread = new();

    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        groupAccessor.Join(Context.ConnectionId, groupName);
    }
    
    public ChannelReader<ChatBubble> StreamMessage(string user, string message,Guid threadId , List<McpServerToSend> availableTools,CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<ChatBubble>();
        _ = Task.Run(() => WriteItemsAsync(channel.Writer, user, message, threadId,availableTools, cancellationToken), cancellationToken);

        //_ = WriteItemsAsync(channel.Writer, user, message, availableTools, cancellationToken);

        return channel.Reader;
    }

    private async Task WriteItemsAsync(
        ChannelWriter<ChatBubble> writer,
      string user, string message, Guid threadId, List<McpServerToSend> availableTools,
        CancellationToken cancellationToken)
    {
        var _kernel = kernel.Clone();
        var totalCompletion = new StringBuilder();

        Exception? localException = null;
        await writer.WriteAsync(new ChatBubble(Guid.NewGuid().ToString(), user, message), cancellationToken);
        var bubbleId = Guid.NewGuid().ToString();
        var executionSettings = new OpenAIPromptExecutionSettings()
        {
            Temperature = 0,
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new() { RetainArgumentTypes = true }),
        };


        ChatCompletionAgent agent =
        new()
        {
            Name = JokerName,
            Instructions = JokerInstructions,
            Kernel = _kernel,
            Arguments = new KernelArguments(executionSettings),
        };
        try
        {
            

             var assistantChatBubble = new ChatBubble(bubbleId, agent.Name, "");
            var alltools = await mcpClientManager.GetSelectedToolsAsync(availableTools, writer, assistantChatBubble, totalCompletion);
           
            agent.Kernel.Plugins.AddFromFunctions("Tools", alltools.Select(aiFunction => aiFunction.AsKernelFunction()));
         
            agent.Kernel.FunctionInvocationFilters.Add(new FunctionCallsFilter(writer,assistantChatBubble , totalCompletion, functionApprovalStore));

            var mcpPrompts = await mcpClientManager.GetSelectedPromptAsync( availableTools, writer, assistantChatBubble, totalCompletion);


            var content = new List<ChatMessageContent>();
 
            if (mcpPrompts != null && mcpPrompts.Any())
            {
                foreach (var mcpPrompt in mcpPrompts)
                {
                    var findArgument = availableTools.Select(x => x.PromptsToSend.FirstOrDefault(z => z.PromptName == mcpPrompt.Name)).FirstOrDefault();
                    GetPromptResult prompts;
                    if (findArgument!= null && findArgument.PromptArgument!= null)
                    {
                        prompts = await mcpPrompt.GetAsync(findArgument.PromptArgument, cancellationToken: cancellationToken);
                    }
                    else
                    {
                        prompts = await mcpPrompt.GetAsync( cancellationToken: cancellationToken);

                    }
                    content.AddRange(prompts.ToChatMessageContents());
                }

            }
            else
            {
                content.Add(new(AuthorRole.User, message));
            }

            // Create a whiteboard provider.
            var chatClient1 = chatCompletionService.AsChatClient();
            var whiteboardProvider = new WhiteboardProvider(chatClient1);

           var chatHistoryReducer = new ChatHistoryTruncationReducer(3, 3);

            // Create a thread for the agent and add the whiteboard to it.
            agentThread.AIContextProviders.Add(whiteboardProvider);



            var agentStreamingResult = agent.InvokeStreamingAsync(content, agentThread,/*options: agentInvokeOptions,*/ cancellationToken:cancellationToken);

            await foreach (var streamingUpdate in agentStreamingResult)
            {
                totalCompletion.Append(streamingUpdate.Message.Content);
                await writer.WriteAsync(new ChatBubble(bubbleId, agent.Name, totalCompletion.ToString()), cancellationToken);
            }
         await agentThread.ChatHistory.ReduceInPlaceAsync(chatHistoryReducer, CancellationToken.None);

            await Clients.All.SendAsync("OnReceiveMessageEnd");
        }
        catch (Exception ex)
        {
            await writer.WriteAsync(new ChatBubble(bubbleId, agent.Name, "Something went wrong, please try again later."), cancellationToken);
        }
        finally
        {

            writer.Complete(localException);
        }
    } 
    public async Task OnCancel()
    {
        await Clients.All.SendAsync("OnReceiveMessageEnd");
    }

}
