using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ProjBobcat.Class.Model;
using SnClient.Auth;
using SnClient.GameBasis;
using SnClient.GameBasis.Auth;
using SnClient.Utils;
using Launcher = SnClient.GameBasis.Launcher;

namespace SnClient.Pages;

public partial class Home : ContentPage
{
    public Home()
    {
        InitializeComponent();
     
        Console.WriteLine("Home page loaded");
        BindingContext = new VersionsViewModel();
        
        // Open Debug Window
        Application.Current.OpenWindow(new Window(new DebugWindow())
        {
            Width = 800,  // Set initial size
            Height = 450,
            X = 100,      // Position on screen
            Y = 100
        });
        
        Core.core.GameLogEventDelegate += (_, args) =>
        {
            DebugLogger.Log($"[{args.LogType}]-[{args.Source}]: {args.Content}");
        };
    }

    public class VersionItem
    {
        public string Name { get; set; }
        public bool Status { get; set; }
        public float StatusOpacity => Status ? 1 : 0.5f;
        public string Id { get; set; }
    }

    public class VersionsViewModel : INotifyPropertyChanged
    {
        private bool isBusy;
        private string errorMessage;
        private bool hasError;

        public ObservableCollection<VersionItem> Versions { get; set; }
        public ICommand PlayCommand { get; }
        public ICommand SwitchAccountCommand { get; }

        public bool IsBusy
        {
            get => isBusy;
            set
            {
                isBusy = value;
                OnPropertyChanged();
            }
        }

        public string ErrorMessage
        {
            get => errorMessage;
            set
            {
                errorMessage = value;
                OnPropertyChanged();
            }
        }

        public bool HasError
        {
            get => hasError;
            set
            {
                hasError = value;
                OnPropertyChanged();
            }
        }
        
        public VersionsViewModel()
        {
            
            // Get all the versions from the server
            var versions = Core.core.VersionLocator.GetAllGames();

            var versionInfos = versions != null ? versions as VersionInfo[] ?? versions.ToArray() :
                new VersionInfo[0];

            Versions = new ObservableCollection<VersionItem>
            {
                new VersionItem { Name = "1.21.0", Status = versionInfos.Count(x => x.RootVersion == "1.21.0") > 0 , Id = "1.21" },
                new VersionItem { Name = "1.20.0", Status = versionInfos.Count(x => x.RootVersion == "1.20.0") > 0 , Id = "1.20" },
                new VersionItem { Name = "1.8.9", Status = versionInfos.Count(x => x.RootVersion == "1.8.9") > 0 , Id = "1.8.9" },
                // Add more versions as needed
            };
            
            IsBusy = false;
            
            // Add a click event to the list
            
            PlayCommand = new Command<string>(OnPlayCommand);
            SwitchAccountCommand = new Command(OnSwitchAccountCommand);
        }
        
        private async void OnPlayCommand(string versionId)
        {
            // Log all versions
            foreach (var version in Core.core.VersionLocator.GetAllGames())
            {
                DebugLogger.Log($"Version: {version.Name} - {version.Id} - {version.RootVersion}");
            }
            
            // Get the game from the server
            var gameInfo = await ForgeLoader.LoadVersion(MCVerions.V1_8_9.ForgeGameVersion);
            await Core.EnsureMinecraftInstalled(MCVerions.V1_8_9.VersionId);
            
            if (gameInfo == null)
            {
                HasError = true;
                ErrorMessage = "Failed to load the game version";
                DebugLogger.Log("Failed to load the game version");
                return;
            }
            else
            {
                HasError = false;
                ErrorMessage = string.Empty;
                Launcher.LaunchGame(gameInfo);
            }
            
            // Implement the logic to play the game
            // For example, launch the game
        }
        
        private async void OnSwitchAccountCommand()
        {
            // Implement the logic to switch account
            // For example, show the account switch dialog
            var res = await MicrosoftAuth.AuthenticateAsync();
            DebugLogger.Log($"Switch account: {res}");
            var tok = await MinecraftAuth.GetMinecraftToken(res);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}