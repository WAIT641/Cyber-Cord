using Shared.Models.ApiModels;

namespace Cyber_Cord.Api.Services;

public interface ICallService
{
    void HandleP2PCall(int callStarter, int callTarget, CallMessageModel model);
}