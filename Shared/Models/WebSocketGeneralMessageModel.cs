using System.Text.Json.Serialization;
using Shared.Types.Interfaces;

namespace Shared.Models;

// General changes to layout (new server, chat, friend..., settins...)
public class WebSocketGeneralMessageModel : IWebSocketMessage
{
    public enum ScopeType
    {
        Server,
        Chat,
        Friend,
        Request,
        Settings
    }
    
    [JsonRequired]
    public ScopeType Scope { get; set; }
}
