using Shared.Models;

namespace Cyber_Cord.App.Services;

public class CallWindowService
{
    public bool Visible { get; private set; }
    private VoiceTokenModel? _token;

    public void MakeCall(VoiceTokenModel token)
    {
        Visible = true;
        _token = token;
    }

    // TODO query participants...

    public void CloseCall()
    {
        Visible = false;
        _token = null;
    }
}
