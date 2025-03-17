namespace SnClient.Utils;

public static class OSHelper
{
    public static string GetOS()
    {
        var os = Environment.OSVersion.VersionString;
        if (os.Contains("Windows"))
        {
            return "windows";
        }
        else if (os.Contains("Linux"))
        {
            return "linux";
        }
        else if (os.Contains("Mac"))
        {
            return "mac";
        }
        else
        {
            return "unknown";
        }
    }

    public static string GetArch()
    {
        return Environment.Is64BitOperatingSystem ? "x64" : "x86";
    }
}