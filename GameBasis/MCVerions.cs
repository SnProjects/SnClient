namespace SnClient.GameBasis;

public interface IMCVersion
{
    public string VersionId { get; }
    public string VersionName { get; }
    public ForgeGameVersion ForgeGameVersion { get; }
}

public class MCVersion : IMCVersion
{
    public string VersionId { get; }
    public string VersionName { get; }
    public ForgeGameVersion ForgeGameVersion { get; }

    public MCVersion(string versionId, string versionName, ForgeGameVersion forgeGameVersion)
    {
        VersionId = versionId;
        VersionName = versionName;
        ForgeGameVersion = forgeGameVersion;
    }
}

public static class MCVerions
{
    // Init 1.8.9 version
    public static IMCVersion V1_8_9 = new MCVersion("1.8.9", "1.8.9", new ForgeGameVersion()
    {
        ForgeVersion = "11.15.1.2318",
        VersionId = "1.8.9",
        JavaVersion = "1.8",
        FullForgeVersion = "1.8.9-11.15.1.2318-1.8.9"
    });
}