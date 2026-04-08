using System.Text.Json.Serialization;
using Shared.Models;
namespace Shared.Types.Interfaces;

[JsonDerivedType(typeof(WebSocketConfigurationModel),   typeDiscriminator: "configuration"  )]
[JsonDerivedType(typeof(WebSocketGeneralMessageModel),  typeDiscriminator: "general"        )]
[JsonDerivedType(typeof(WebSocketMessageActionModel),   typeDiscriminator: "action"         )]
[JsonDerivedType(typeof(WebSocketPingModel),            typeDiscriminator: "ping"           )]
[JsonDerivedType(typeof(WebSocketServerModel),          typeDiscriminator: "server"         )]
[JsonDerivedType(typeof(WebSocketCallMessageModel),     typeDiscriminator: "call"           )]
public interface IWebSocketMessage { }
