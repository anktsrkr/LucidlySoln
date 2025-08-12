using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Lucidly.API.HistoryProvider
{
    internal interface IChatHistoryProvider
    {
        /// <summary>
        /// Provides access to the chat history.
        /// </summary>
        Task<ChatHistory> GetHistoryAsync();

        /// <summary>
        /// Commits any updates to the chat history.
        /// </summary>
        Task CommitAsync(ChatMessageContent chatMessageContent);
    }
    internal sealed class ChatHistoryProvider() : IChatHistoryProvider
    {
        private readonly ChatHistory history = [];
        /// <inheritdoc/>
        public Task<ChatHistory> GetHistoryAsync() => Task.FromResult(history);

        /// <inheritdoc/>
        public Task CommitAsync(ChatMessageContent chatMessageContent)
        {
            history.Add(chatMessageContent);
            return Task.CompletedTask;
        }
    }
}
