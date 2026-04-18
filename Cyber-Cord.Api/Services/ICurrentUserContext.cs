namespace Cyber_Cord.Api.Services;

public interface ICurrentUserContext
{
    public int GetId();
    public string GetName();
    public string GetDisplayName();
}
