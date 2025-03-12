using ProjBobcat.Class.Helper;
using SnClient.GameBasis;
using System.Net;

namespace SnClient
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            Sharpnado.MaterialFrame.Initializer.Initialize(loggerEnable: false, debugLogEnable: false);

            ServicePointManager.DefaultConnectionLimit = 512;

            /*
             Initialize the basic helpers of Projbobcat.
             */
            ServiceHelper.Init();
            HttpClientHelper.Init();

            // Initialize the launcher core.
            Core.CoreInit();

            MainPage = new AppShell();
        }
    }
}
