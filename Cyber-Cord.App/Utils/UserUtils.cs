namespace Cyber_Cord.App.Utils;

public static class UserUtils
{
    public static string GetUserInitials(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return string.Empty;
        }

        return name[..Math.Min(2, name.Length)].ToUpper();
    }
}
