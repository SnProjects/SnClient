# SnClient

## Overview
SnClient is an open-source Minecraft client designed to enhance the gaming experience by improving performance and offering built-in modes tailored for PvP (Player vs. Player) and PvE (Player vs. Environment) gameplay. Inspired by clients like Lunar Client and Badlion Client, SnClient aims to provide a lightweight, customizable, and high-performance alternative while maintaining transparency through its open-source nature. Built with .NET MAUI and integrated with ProjBobcat for seamless Minecraft launching, SnClient leverages modern authentication (via MSAL) and Forge mod support to deliver a robust platform for Minecraft enthusiasts.

## Features
- **Performance Optimization**: Optimized rendering and resource management for smoother gameplay, even on lower-end devices.
- **Built-in PvP Presets**: Enhances combat mechanics, including hit registration, and customizable HUDs for competitive play.
- **Built-in PvE Presets**: Improves exploration and survival.
- **Open Source**: Fully transparent codebase, encouraging community contributions and customizations.
- **Forge Integration**: Supports Forge mods (e.g., 1.8.9-forge1.8.9-11.15.1.2318) for extended functionality.
- **Modern Authentication**: Uses MSAL for secure Microsoft account login with persistent caching.
- **Cross-Platform**: Built with .NET MAUI, targeting Windows, macOS, and potentially other platforms.
- **Customizable**: Extensible architecture for adding new features or modes.

## Getting Started

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [ProjBobcat NuGet Package](https://www.nuget.org/packages/ProjBobcat.Classic/)
- [Microsoft.Identity.Client NuGet Package](https://www.nuget.org/packages/Microsoft.Identity.Client/)
- Java Runtime Environment (JRE) for Minecraft (bundled or installed separately)
- Internet connection for authentication and updates

### Installation
1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/SnClient.git
   cd SnClient
   ```
2. Restore dependencies:
   ```bash
   dotnet restore
   ```
3. Build the solution:
   ```bash
   dotnet build
   ```
4. Run the application:
   ```bash
   dotnet run --project SnClient
   ```

### Configuration
- Update the `ClientId` in `MicrosoftAuth.cs` with your Azure AD application ID (register at [Azure Portal](https://portal.azure.com)).
- Ensure the `RedirectUri` (`http://localhost`) matches your app registration.
- Set the `Core.rootPath` in your code to the desired Minecraft directory (e.g., `.minecraft`).

## Usage
1. Launch the app and log in with your Microsoft account (prompted on first run).
2. Select a version (e.g., 1.8.9 Forge) and choose a mode (PvP or PvE).
3. Customize settings via the in-game menu (to be implemented).
4. Enjoy enhanced gameplay with optimized performance and mode-specific features.

## Development
### Contributing
SnClient is open-source and welcomes contributions! To contribute:
1. Fork the repository.
2. Create a feature branch (`git checkout -b feature/your-feature`).
3. Commit your changes (`git commit -m "Add your feature"`).
4. Push to the branch (`git push origin feature/your-feature`).
5. Open a pull request.

### Roadmap
- [ ] Implement PvP mode with aim assist and HUD customization.
- [ ] Develop PvE mode with auto-farming and mob tracking.
- [ ] Add performance profiling tools.
- [ ] Support additional Minecraft versions and mods.
- [ ] Create a user-friendly settings UI.

### Known Issues
- Initial login may require manual cache clearing if authentication fails (use `ProjBobcatAuth.ClearCacheAsync()`).
- PvP/PvE modes are under development—expect placeholder functionality.

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments
- Inspired by Lunar Client and Badlion Client.
- Built with [ProjBobcat](https://github.com/ProjBobcat/ProjBobcat) for Minecraft launching.
- Utilizes [MSAL](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet) for authentication.

## Contact
For questions or support, open an issue on the [GitHub repository](https://github.com/yourusername/SnClient/issues) or contact the maintainers.
