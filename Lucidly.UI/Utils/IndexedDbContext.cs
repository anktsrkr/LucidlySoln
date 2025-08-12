using Lucidly.Common;
using Magic.IndexedDb;
using Magic.IndexedDb.Interfaces;
using Magic.IndexedDb.SchemaAnnotations;
using ModelContextProtocol.Protocol;

namespace Lucidly.UI.Utils
{
    public class IndexedDbContext : IMagicRepository
    {
        public static readonly IndexedDbSet McpServers= new("McpServers");
      
    }

    public class ProtectedMcpServer : MagicTableTool<ProtectedMcpServer>, IMagicTable<ProtectedMcpServer.DbSets>
    {
        public Guid UniqueGuid { get; set; } = Guid.NewGuid();

        public IMagicCompoundKey GetKeys() =>
            CreatePrimaryKey(x => x.UniqueGuid, false); // Auto-incrementing primary key

        public List<IMagicCompoundIndex>? GetCompoundIndexes() => [];

        public string GetTableName() => "McpServers";
        public IndexedDbSet GetDefaultDatabase() => IndexedDbContext.McpServers;

        public byte[] Value  { get; set; }


        [MagicNotMapped]
        public DbSets Databases { get; } = new();
        public sealed class DbSets
        {
            public readonly IndexedDbSet McpServers = IndexedDbContext.McpServers;
        }
    }


    public class ToolToSave 
    {
        public bool Selected { get; set; }
        public Tool Tool { get; set; }
    }
    public class PromptToSave
    {
        public bool Selected { get; set; }
        public Prompt   Prompt { get; set; }
    }
    public class ProtectedMcpTools : MagicTableTool<ProtectedMcpTools>, IMagicTable<ProtectedMcpTools.DbSets>
    {
        public Guid UniqueGuid { get; set; } = Guid.NewGuid();
        [MagicIndex]
        public Guid ServerId { get; set; }

        public IMagicCompoundKey GetKeys() =>
            CreatePrimaryKey(x => x.UniqueGuid, false); // Auto-incrementing primary key

        public List<IMagicCompoundIndex>? GetCompoundIndexes() => [];

        public string GetTableName() => "McpTools";
        public IndexedDbSet GetDefaultDatabase() => IndexedDbContext.McpServers;

        public byte[] Value { get; set; }


        [MagicNotMapped]
        public DbSets Databases { get; } = new();
        public sealed class DbSets
        {
            public readonly IndexedDbSet McpTools = IndexedDbContext.McpServers;
        }
    }
    public class ProtectedMcpPrompts : MagicTableTool<ProtectedMcpPrompts>, IMagicTable<ProtectedMcpPrompts.DbSets>
    {
        public Guid UniqueGuid { get; set; } = Guid.NewGuid();
        [MagicIndex]
        public Guid ServerId { get; set; }

        public IMagicCompoundKey GetKeys() =>
            CreatePrimaryKey(x => x.UniqueGuid, false); // Auto-incrementing primary key

        public List<IMagicCompoundIndex>? GetCompoundIndexes() => [];

        public string GetTableName() => "McpPrompts";
        public IndexedDbSet GetDefaultDatabase() => IndexedDbContext.McpServers;

        public byte[] Value { get; set; }


        [MagicNotMapped]
        public DbSets Databases { get; } = new();
        public sealed class DbSets
        {
            public readonly IndexedDbSet McpPrompts = IndexedDbContext.McpServers;
        }
    }

    
    public class McpServer
    {
      
        public string Name { get; set; }
        public MCPTransportMode Type { get; set; }
        public string Uri { get; set; }
        public Dictionary<string, string> AdditionalHeaders { get; set; } = new();
        public TokenContainer TokenContainer { get; set; }

        public List<ToolToSave> Tools { get; set; } = [];
    }
}


public class TokenContainer
{
    public string AccessToken { get; set; }
}
