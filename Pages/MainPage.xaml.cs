using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using SnClient.GameBasis;
using ProjBobcat.Class.Model;
using ProjBobcat.Class.Model.LauncherProfile;
using ProjBobcat.DefaultComponent;
using ProjBobcat.DefaultComponent.Authenticator;
using ProjBobcat.DefaultComponent.ResourceInfoResolver;
using ProjBobcat.Event;
using ProjBobcat.Interface;

namespace SnClient.Pages
{
    public partial class MainPage : ContentPage
    {
        private List<VersionInfo> gameList;
        private IEnumerable<string> javaList;

        public MainPage()
        {
            InitializeComponent();

            // Necessary UI preparation
            RefreshJavaList();
            RefreshGameList();
        }

        /// <summary>
        ///     Refreshes the list of Java(s) installed.
        /// </summary>
        private async void RefreshJavaList()
        {
            JavaListView.ItemsSource = javaList;
        }

        /// <summary>
        ///     Refreshes the list of Game(s) in .minecraft/ directory.
        /// </summary>
        private void RefreshGameList()
        {
            gameList = Core.core.VersionLocator.GetAllGames().ToList();
            GameListView.ItemsSource = gameList;
        }

        private async Task DownloadResourcesAsync(VersionInfo versionInfo)
        {
            /*var versions = await Core.GetVersionManifestTaskAsync();
            var rc = new DefaultResourceCompleter
            {
                CheckFile = true,
                DownloadParts = 8,
                ResourceInfoResolvers = new List<IResourceInfoResolver>
                {
                    new VersionInfoResolver
                    {
                        BasePath = Core.core.RootPath,
                        VersionInfo = versionInfo,
                        CheckLocalFiles = true
                    },
                    new AssetInfoResolver
                    {
                        BasePath = Core.core.RootPath,
                        VersionInfo = versionInfo,
                        CheckLocalFiles = true,
                        Versions = versions?.Versions,
                    },
                    new LibraryInfoResolver
                    {
                        BasePath = Core.core.RootPath,
                        VersionInfo = versionInfo,
                        CheckLocalFiles = true,
                    }
                },
                TotalRetry = 2
            };

            rc.DownloadFileCompletedEvent += (sender, args) =>
            {
                if (sender is not DownloadFile file) return;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    RcProgress.Progress = (double)rc.TotalDownloaded / rc.NeedToDownload;
                });
            };

            await rc.CheckAndDownloadTaskAsync();*/
        }

        private void RefJavaBtn_Click(object sender, EventArgs e)
        {
            RefreshJavaList();
        }

        private void RefGameListBtn_Click(object sender, EventArgs e)
        {
            RefreshGameList();
        }

        private async void LaunchBtn_Click(object sender, EventArgs e)
        {
            if (GameListView.SelectedItem is null)
            {
                await DisplayAlert("Error", "Select java and a game first.", "OK");
                return;
            }

            if (GameListView.SelectedItem is not VersionInfo versionInfo) return;

            var launchSettings = new LaunchSettings
            {
                FallBackGameArguments = new GameArguments
                {
                    GcType = GcType.G1Gc,
                    JavaExecutable = "C:\\Users\\snowy\\.lunarclient\\jre\\56e53accb20696f802d92bd011174126b5e3154e\\zulu21.30.15-ca-jre21.0.1-win_x64\\bin\\javaw.exe",
                    Resolution = new ResolutionModel
                    {
                        Height = 600,
                        Width = 800
                    },
                    MinMemory = 512,
                    MaxMemory = 1024
                },
                Version = versionInfo.Id,
                GameName = versionInfo.Name,
                VersionInsulation = false,
                GameResourcePath = Core.core.RootPath,
                GamePath = Core.core.RootPath,
                VersionLocator = Core.core.VersionLocator,
                Authenticator = new OfflineAuthenticator //Offline authentication
                {
                    Username = OfflUN.Text, //Offline username
                    LauncherAccountParser = Core.core.VersionLocator.LauncherAccountParser
                },
                
                GameArguments = new GameArguments
                {
                    GcType = GcType.G1Gc,
                    JavaExecutable = javaList.First(),
                    Resolution = new ResolutionModel
                    {
                        Height = 600,
                        Width = 800
                    },
                    MinMemory = 512,
                    MaxMemory = 1024
                }
            };

            await DownloadResourcesAsync(versionInfo);

            Core.core.LaunchLogEventDelegate += Core_LaunchLogEventDelegate;
            Core.core.GameLogEventDelegate += Core_GameLogEventDelegate;
            Core.core.GameExitEventDelegate += Core_GameExitEventDelegate;

            var result = await Core.core.LaunchTaskAsync(launchSettings);

            if (result.Error != null)
            {
                GameLaunchLogs.Text += result.Error.Exception.ToString();
            }
        }

        private void Core_GameExitEventDelegate(object sender, GameExitEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() => { 
                GameLaunchLogs.Text += "Game exited."; 
            });
        }

        private void Core_GameLogEventDelegate(object sender, GameLogEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() => { 
                GameLaunchLogs.Text += $"[Game Log] - {e.Content}\n"; 
            });
        }

        private void Core_LaunchLogEventDelegate(object sender, LaunchLogEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() => { 
                GameLaunchLogs.Text += $"[Bobcat Log] - {e.Item}\n"; 
            });
        }
    }
}