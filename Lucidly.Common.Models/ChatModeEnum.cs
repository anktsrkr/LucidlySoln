namespace Lucidly.Common;

public enum ChatModeEnum
{
    Solo,
    Collaborative,
}
public enum MCPServerMode
{
  STDIO,
  HTTP
}
public enum MCPTransportMode
{
    Sse,
    StreamableHttp
}

public enum McpClientRegistrationMode
{
    Dynamic,
    Static
}

public enum McpClientAuthenticationMode
{
   NoAuth 
    , OAuth,
    ApiKey
}


public record McpTool(
    string Name,
    string Description
); public record McpPrompt(
    string Name,
    string Description
);