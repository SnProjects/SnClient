
using ProjBobcat.Class.Model;
using ProjBobcat.Class.Model.LauncherProfile;
using ProjBobcat.DefaultComponent.Authenticator;
using SnClient.Utils;

namespace SnClient.GameBasis;

public static class Launcher
{
    public static async void LaunchGame(VersionInfo gameVersion)
    {
        string javaPath = await ReadyUpGame(gameVersion);
        if (string.IsNullOrEmpty(javaPath))
        {
            DebugLogger.Log("Failed to get Java path.");
            return;
        }
        
        // launch game
        var launchSettings = new LaunchSettings
        {
            GameName = gameVersion.Id, // Game Name
            FallBackGameArguments = new GameArguments // Default game arguments for all games in .minecraft/ as the fallback of specific game launch.
            {
                GcType = GcType.G1Gc, // GC type
                JavaExecutable = javaPath, //The path of Java executable
                Resolution = new ResolutionModel // Game Window's Resolution
                {
                    Height = 600, // Height
                    Width = 800 // Width
                },
                MinMemory = 512, // Minimal Memory
                MaxMemory = 1024 // Maximum Memory
            },
            Version = gameVersion.Id, // The version ID of the game to launch, such as 1.7.10 or 1.15.2
            VersionInsulation = false, // Version Isolation
            GameResourcePath = Core.core.RootPath, // Root path of the game resource(.minecraft/)
            GamePath = gameVersion.DirName, // Root path of the game (.minecraft/versions/)
            VersionLocator = Core.core.VersionLocator, // Game's version locator
            
            Authenticator = new OfflineAuthenticator //Offline authentication
            {
                Username = "OfflineUsername", //Offline username
                LauncherAccountParser = Core.core.VersionLocator.LauncherAccountParser
            },
            
            GameArguments = new GameArguments // Game Arguments
            {
                GcType = GcType.G1Gc, // GC type
                JavaExecutable = javaPath, //The path of Java executable
                Resolution = new ResolutionModel // Game Window's Resolution
                {
                    Height = 600, // Height
                    Width = 800 // Width
                },
                MinMemory = 512, // Minimal Memory
                MaxMemory = 1024 // Maximum Memory
            }
        };
        
        DebugLogger.Log("Launching game...");
        
        // Launch the game
        // Hide the game window until the game is ready to play
        var result = await Core.core.LaunchTaskAsync(launchSettings).ConfigureAwait(false);
        
        if (result.Error != null)
        {
            DebugLogger.Log(result.Error.Exception.ToString());
        }
    }

    private static async Task<string> ReadyUpGame(VersionInfo gameVersion)
    {
        // download resources
        await Core.DownloadResourcesAsync(gameVersion);
        
        DebugLogger.Log($"Required java version is: {gameVersion.JavaVersion?.MajorVersion} ---- {gameVersion.JavaVersion?.Component}");
        string javaPath = await JavaDetection.GetJava(gameVersion?.JavaVersion?.MajorVersion ?? 21);
        
        if (string.IsNullOrEmpty(javaPath))
        {
            DebugLogger.Log("Failed to get Java path.");
            return "";
        }
        
        // check if the java path is correct
        if (!File.Exists(javaPath))
        {
            DebugLogger.Log("Java path is incorrect.");
            return "";
        }
        
        DebugLogger.Log($"Java path: {javaPath}");
        
        return javaPath;
    }
}