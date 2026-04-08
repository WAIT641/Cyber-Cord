using System.Collections.Concurrent;
using Cyber_Cord.Api.Runtime;
using Shared.Models;
using Shared.Models.ApiModels;

namespace Cyber_Cord.Api.Services;

public class CallService(
    IWebSocketHandler webSocketHandler
    ) : ICallService
{
    public void HandleP2PCall(int callStarter, int callTarget, CallMessageModel model)
    {
        switch (model.Type)
        {
            case CallMessageModel.MessageType.Start:
                SendMessage(WebSocketCallMessageModel.MessageType.CallOffer, callTarget);
                break;
            case CallMessageModel.MessageType.End:
                SendMessage(WebSocketCallMessageModel.MessageType.CallEnded, callTarget);
                break;
            case CallMessageModel.MessageType.Reject:
                SendMessage(WebSocketCallMessageModel.MessageType.CallRejected, callTarget);
                break;
            case CallMessageModel.MessageType.Accept:
                SendMessage(WebSocketCallMessageModel.MessageType.CallAnswer, callTarget);
                break;
        }
    }

    private void SendMessage(WebSocketCallMessageModel.MessageType type, int userId)
    {
        var message = new WebSocketCallMessageModel()
        {
            Type = WebSocketCallMessageModel.MessageType.CallOffer
             
        };
        
        webSocketHandler.SendToUser(userId, message);
    }
}