using Microsoft.Identity.Client;
using SnClient.GameBasis;
using SnClient.Utils;

namespace SnClient.Auth;

public class MicrosoftAuth
{
    private static readonly string ClientId = "7bb959e2-77e1-4f88-b2a0-7dfd50b9dca1"; // Your Client ID

    private static readonly string[] Scopes = new[]
        { "XboxLive.signin", "offline_access", "openid", "profile", "email" };
    private static readonly string RedirectUri = $"http://localhost"; // Consistent redirect URI
    private static readonly string CacheFilePath = Path.Combine(Core.rootPath, "msal_cache.bin");
    private static readonly object FileLock = new object();
    
    public static IPublicClientApplication PublicClientApp { get; private set; }

    static MicrosoftAuth()
    {
        var builder = PublicClientApplicationBuilder
            .Create(ClientId)
            .WithAuthority("https://login.microsoftonline.com/consumers")
            .WithRedirectUri(RedirectUri);
        PublicClientApp = builder.Build();

        // Hook into token cache for persistence
        PublicClientApp.UserTokenCache.SetBeforeAccess(BeforeCacheAccess);
        PublicClientApp.UserTokenCache.SetAfterAccess(AfterCacheAccess);
    }

    private static void BeforeCacheAccess(TokenCacheNotificationArgs args)
    {
        lock (FileLock)
        {
            if (File.Exists(CacheFilePath))
            {
                var cacheBytes = File.ReadAllBytes(CacheFilePath);
                args.TokenCache.DeserializeMsalV3(cacheBytes);
                DebugLogger.Log("Loaded MSAL token cache from file");
            }
            else
            {
                DebugLogger.Log("No MSAL cache file found");
            }
        }
    }

    private static void AfterCacheAccess(TokenCacheNotificationArgs args)
    {
        if (args.HasStateChanged)
        {
            lock (FileLock)
            {
                var cacheBytes = args.TokenCache.SerializeMsalV3();
                File.WriteAllBytes(CacheFilePath, cacheBytes);
                DebugLogger.Log("Saved MSAL token cache to file");
            }
        }
    }

    public static async Task<AuthenticationResult> AuthenticateAsync()
    {
        try
        {
            var accounts = await PublicClientApp.GetAccountsAsync();
            DebugLogger.Log($"Cached accounts: {accounts.Count()}");
            if (accounts.Any())
            {
                var account = accounts.FirstOrDefault();
                DebugLogger.Log($"Attempting silent auth for account: {account?.Username ?? "unknown"}");
                var silentResult = await PublicClientApp
                    .AcquireTokenSilent(Scopes, account)
                    .ExecuteAsync();
                DebugLogger.Log("Silently acquired Microsoft token");
                return silentResult;
            }
            else
            {
                DebugLogger.Log("No cached accounts found.");
            }
        }
        catch (MsalUiRequiredException ex)
        {
            DebugLogger.Log($"Silent auth failed, prompting user: {ex.Message}");
        }

        try
        {
            DebugLogger.Log("Prompting user for Microsoft login...");
            DebugLogger.Log($"Scopes: {string.Join(", ", Scopes)}, RedirectUri: {PublicClientApp.AppConfig.RedirectUri}");

            var interactiveResult = await PublicClientApp
                .AcquireTokenInteractive(Scopes)
                .ExecuteAsync(); // No window handle needed for browser flow

            DebugLogger.Log($"{interactiveResult.Account.Username} - {interactiveResult.TokenType} - {interactiveResult.ExpiresOn}");
            
            return interactiveResult;
        }
        catch (Exception ex)
        {
            DebugLogger.Log($"Microsoft auth failed: {ex.Message}");
            DebugLogger.Log($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }
}