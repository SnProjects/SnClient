using System;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ProjBobcat.Class.Helper;
using ProjBobcat.Class.Model;
using ProjBobcat.Class.Model.MicrosoftAuth;
using ProjBobcat.Class.Model.Mojang;
using ProjBobcat.DefaultComponent;
using ProjBobcat.DefaultComponent.Authenticator;
using ProjBobcat.DefaultComponent.Launch;
using ProjBobcat.DefaultComponent.Launch.GameCore;
using ProjBobcat.DefaultComponent.Logging;
using ProjBobcat.DefaultComponent.ResourceInfoResolver;
using ProjBobcat.Interface;
using SnClient.Utils;

namespace SnClient.GameBasis
{
    public static class Core
    {
        public static DefaultGameCore core;
        public static string gameRootPath;
        public static string rootPath;
        public static Guid clientToken;
        
        public static Dictionary<string, string> extractedLibraries = new Dictionary<string, string>();

        public static void CoreInit()
        {
            // configure rootPath should be in the user's folder ex: C:\Users\{username}\.snclient
            rootPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\.snclient";
            if (!Directory.Exists(rootPath))
            {
                Directory.CreateDirectory(rootPath);
            }
            
            // path to .minecraft
            gameRootPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft";
            clientToken = Guid.NewGuid();
            core = new DefaultGameCore
            {
                ClientToken = clientToken, // Pick any GUID as you like, and it does not affect launching.
                RootPath = gameRootPath,
                VersionLocator = new DefaultVersionLocator(gameRootPath, clientToken)
                {
                    LauncherProfileParser = new DefaultLauncherProfileParser(gameRootPath, clientToken),
                    LauncherAccountParser = new DefaultLauncherAccountParser(gameRootPath, clientToken)
                },
                GameLogResolver = new DefaultGameLogResolver()
            };

            LoadExtractResources();

            ConfigureAuth();
        }

        private static void LoadExtractResources()
        {
            // Check if the libraries folder exists
            var libPath = Path.Combine(rootPath, "libraries");
            if (!Directory.Exists(libPath))
            {
                Directory.CreateDirectory(libPath);
            }
            
            // check for all folders in the libraries folder
            var libFolders = Directory.GetDirectories(libPath);
            foreach (var folder in libFolders)
            {
                // dirname is the resource name, then the full path of the first file in the folder
                var dirname = Path.GetFileName(folder);
                var files = Directory.GetFiles(folder);
                if (files.Length == 0) continue;
                
                var file = files[0];
                extractedLibraries.Add(dirname, file);
            }
        }
        
        public static async Task<string> GetExtractedResource(string resourceName, string fileName)
        {
            // Check if the resource is already extracted
            if (extractedLibraries.TryGetValue(resourceName, out var resource))
            {
                return resource;
            }
            
            // Extract the resource
            var libPath = Path.Combine(rootPath, "libraries", resourceName);
            if (!Directory.Exists(libPath))
            {
                Directory.CreateDirectory(libPath);
            }

            await ExtractEmbeddedResource(resourceName, Path.Combine(libPath, fileName));
            extractedLibraries.Add(resourceName, Path.Combine(libPath, fileName));
            return Path.Combine(libPath, fileName);
        }
        
        private static async Task ExtractEmbeddedResource(string resourceName, string outputPath)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new Exception($"Embedded resource '{resourceName}' not found.");

                using (var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                {
                    await stream.CopyToAsync(fileStream);
                }
            }
        }

        public static async Task<VersionManifest?> GetVersionManifestTaskAsync()
        {
            const string vmUrl = "http://launchermeta.mojang.com/mc/game/version_manifest.json";
            var contentRes = await HttpHelper.Get(vmUrl);
            var content = await contentRes.Content.ReadAsStringAsync();
            var model = JsonConvert.DeserializeObject<VersionManifest>(content);

            return model;
        }
        
        public static async Task DownloadResourcesAsync(VersionInfo versionInfo)
        {
            var versions = await GetVersionManifestTaskAsync();
            var rc = new DefaultResourceCompleter
            {
                CheckFile = true,
                DownloadParts = 8,
                ResourceInfoResolvers = new List<IResourceInfoResolver>
                {
                    new VersionInfoResolver
                    {
                        BasePath = core.RootPath,
                        VersionInfo = versionInfo,
                        CheckLocalFiles = true
                    },
                    new AssetInfoResolver
                    {
                        BasePath = core.RootPath,
                        VersionInfo = versionInfo,
                        CheckLocalFiles = true,
                        Versions = versions?.Versions
                    },
                    new LibraryInfoResolver
                    {
                        BasePath = core.RootPath,
                        VersionInfo = versionInfo,
                        CheckLocalFiles = true,
                    }
                },
                MaxDegreeOfParallelism = 8,
                TotalRetry = 2
            };
            
            rc.GameResourceInfoResolveStatus += (sender, args) =>
            {
                DebugLogger.Log($"Downloaded {args.Progress} - {args.Status}");
            };

            await rc.CheckAndDownloadTaskAsync();
            
            DebugLogger.Log("Downloaded all resources.");
        }
        
        public static async Task EnsureMinecraftInstalled(string versionId)
        {
            var versions = core.VersionLocator.GetAllGames().ToList();
            if (versions.FirstOrDefault(x => x.RootVersion == versionId) == default)
            {
                var mainfest = await GetVersionManifestTaskAsync();
                var versionInfo = mainfest?.Versions.FirstOrDefault(x => x.Id == versionId);
                if (versionInfo == null)
                {
                    DebugLogger.Log($"Version {versionId} not found in the manifest.");
                    return;
                }
                
                // Get the url
                var versionUrl = versionInfo.Url;
                var versionContentRes = await HttpHelper.Get(versionUrl);
                var versionContent = await versionContentRes.Content.ReadAsStringAsync();
                
                // save the info as a json file inside the versions folder
                var versionPath = Path.Combine(gameRootPath, "versions", versionId);
                if (!Directory.Exists(versionPath))
                {
                    Directory.CreateDirectory(versionPath);
                }
                
                var versionJsonPath = Path.Combine(versionPath, versionId + ".json");
                await File.WriteAllTextAsync(versionJsonPath, versionContent);
                
                // try get the version info
                var newInfo = core.VersionLocator.GetGame(versionId);
                if (newInfo == null)
                {
                    DebugLogger.Log($"Failed to get version info for {versionId}");
                    return;
                }
                
                // download the resources
                await DownloadResourcesAsync(newInfo);
            }
        }
        
        
        private static void ConfigureAuth()
        {
            MicrosoftAuthenticator.Configure(new MicrosoftAuthenticatorAPISettings
            {
                ClientId = "7bb959e2-77e1-4f88-b2a0-7dfd50b9dca1",
                TenentId = "consumers",
                Scopes = new[] { "XboxLive.signin",  "offline_access",  "openid",  "profile",  "email" }
            });
        }
    }
}