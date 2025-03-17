using Newtonsoft.Json;
using ProjBobcat.Class.Helper;
using ProjBobcat.Class.Model.Fabric;

namespace SnClient.GameBasis;

public static class FabricInstaller
{
    public static event Action<double, string>? ProgressChangedEvent;
    
    public static bool IsVersionInstalled(string version)
    {
        var games = Core.core.VersionLocator.GetAllGames();
        return games.Any(game => game.RootVersion == version);
    }
    
    public static async Task InstallFabricAsync(string version)
    {
        // Send a request to https://meta.fabricmc.net/v2/versions/loader/[version] to get the loader version.
        var artifact = await GetLoaderVersionAsync(version);
        
        // print the loader version
        Console.WriteLine($"Loader version: {artifact.Loader.Version}");
        
        // Download the loader jar file
        var fabricInstaller = new ProjBobcat.DefaultComponent.Installer.FabricInstaller()
        {
            LoaderArtifact = artifact,
            RootPath = Core.gameRootPath + "/versions",
            VersionLocator = Core.core.VersionLocator,
        };
        
        await fabricInstaller.InstallTaskAsync();
        
        fabricInstaller.StageChangedEventDelegate += (_, args) =>
        {
            ProgressChangedEvent?.Invoke(args.Progress, args.CurrentStage);
        };
    }
    
    public static async Task<FabricLoaderArtifactModel> GetLoaderVersionAsync(string version)
    {
        var url = $"https://meta.fabricmc.net/v2/versions/loader/1.19.2";
        Console.WriteLine($"Requesting {url}");
        var response = await HttpHelper.Get(url);
        var responseJson = await response.Content.ReadAsStringAsync();
        
        Console.WriteLine($"Response: {responseJson}");
        
        // conver json to object
        var artifacts = JsonConvert.DeserializeObject<List<FabricLoaderArtifactModel>>(responseJson);
        
        if (artifacts != null)
        {
            // Return the first artifact
            return artifacts.First();
        } else
        {
            throw new Exception("Failed to get loader version");
        }
    }
    
    public static async Task<string> GetLoaderVersionAsyncTest(string version)
    {
        var url = $"https://meta.fabricmc.net/v2/versions/loader/{version}";
        Console.WriteLine($"Requesting {url}");
        var response = await HttpHelper.Get(url);
        var responseJson = await response.Content.ReadAsStringAsync();
        
        Console.WriteLine($"Response: {responseJson}");
        
        return responseJson;
    }
}