using ProjBobcat.DefaultComponent.Installer.ForgeInstaller;
using ProjBobcat.Interface;
using System.Reflection;
using ProjBobcat.Class.Model;
using SnClient.Utils;

namespace SnClient.GameBasis;

public class ForgeGameVersion
{
    public string VersionId { get; set; } = "1.8.9";
    public string ForgeVersion { get; set; } = "11.15.1.2318";
    public string FullForgeVersion { get; set; } = "1.8.9-11.15.1.2318-1.8.9";
    public string JavaVersion { get; set; } = "8";
}

public static class ForgeLoader
{
    private static readonly string ForgeResourceName = "SnClient.Resources.JARS.Forge.forge-{0}.jar"; // Adjust namespace

    public static async Task<VersionInfo?> LoadVersion(ForgeGameVersion gameVersion)
    {
        // Check if the game version is already installed
        string customId = $"SnClient-{gameVersion.VersionId}-Forge";
        if (Core.core.VersionLocator.GetGame(customId) != null)
        {
            DebugLogger.Log($"Game version {customId} is already installed");
            return Core.core.VersionLocator.GetGame(customId);
        }
        
        try
        {
            DebugLogger.Log($"Starting Forge installation for {customId}");
            // Extract the embedded Forge installer to a temp file
            var installerPath = await Core.GetExtractedResource(ForgeResourceName.Replace("{0}", gameVersion.VersionId), "forge.jar");
            
            DebugLogger.Log($"Extracted Forge installer to {installerPath}");

            var forgeVersionArtifact =
                ForgeInstallerFactory.GetForgeArtifactVersion(gameVersion.VersionId, gameVersion.ForgeVersion);
            var isLegacy = ForgeInstallerFactory.IsLegacyForgeInstaller(installerPath, forgeVersionArtifact);

            DebugLogger.Log($"Forge version: {gameVersion.FullForgeVersion} - Legacy: {isLegacy}");

            IForgeInstaller forgeInstaller = isLegacy
                ? new LegacyForgeInstaller
                {
                    ForgeExecutablePath = installerPath,
                    RootPath = Core.gameRootPath,
                    CustomId = $"SnClient-{gameVersion.VersionId}-Forge",
                    ForgeVersion = forgeVersionArtifact,
                    InheritsFrom = gameVersion.VersionId
                }
                : new HighVersionForgeInstaller
                {
                    ForgeExecutablePath = installerPath,
                    JavaExecutablePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "jre", "bin", "java.exe"),
                    RootPath = Core.gameRootPath,
                    VersionLocator = Core.core.VersionLocator,
                    DownloadUrlRoot = "https://bmclapidoc.bangbang93.com/",
                    CustomId = $"SnClient-{gameVersion.VersionId}-Forge",
                    MineCraftVersion = gameVersion.VersionId,
                    MineCraftVersionId = gameVersion.VersionId,
                    InheritsFrom = gameVersion.VersionId
                };

            ((InstallerBase)forgeInstaller).StageChangedEventDelegate += (_, args) =>
            {
                // args.Progress * 100,  args.CurrentStage
                DebugLogger.Log($"Progress: {args.Progress * 100}% - {args.CurrentStage}");
            };

            var result = await forgeInstaller.InstallForgeTaskAsync();
            
            if (!result.Succeeded)
            {
                DebugLogger.Log($"Forge installation failed: {result.Error?.ErrorMessage}");
                DebugLogger.Log($"{result.Error?.Error} - {result.Error?.Cause} - {result.Error?.ErrorMessage}");
                DebugLogger.Log($"{result.Error?.Exception}");
                return null;
            } else {
                DebugLogger.Log($"Forge installation succeeded");
            }
            
            return Core.core.VersionLocator.GetGame(gameVersion.VersionId);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");
            throw; // TODO: Handle properly
        }
    }
    
    
}