using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Identity.Client;
using SnClient.Utils;

namespace SnClient.Auth;

public class AuthXSTSResponseModel
{
    public DateTime IssueInstant { get; set; }
    public DateTime NotAfter { get; set; }
    public required string Token { get; set; }
    public JsonElement DisplayClaims { get; set; }
}

[JsonSerializable(typeof(ProjBobcat.Class.Model.MicrosoftAuth.AuthXSTSResponseModel))]
partial class AuthXSTSResponseModelContext : JsonSerializerContext
{
}

public class MinecraftAuth
{
    private static readonly HttpClient HttpClient = new HttpClient();

    private const string XBOX_LIVE_AUTH_URL = "https://user.auth.xboxlive.com/user/authenticate";
    private const string XSTS_AUTH_URL = "https://xsts.auth.xboxlive.com/xsts/authorize";
    private const string MOJANG_AUTH_URL = "https://api.minecraftservices.com/authentication/login_with_xbox";
    private const string MOJANG_OWNERSHIP_URL = "https://api.minecraftservices.com/entitlements/mcstore";
    private const string MOJANG_PROFILE_URL = "https://api.minecraftservices.com/minecraft/profile";

    public static async Task<(string userHash, string mcToken, string xuid, string username, string uuid)> GetMinecraftToken(AuthenticationResult msResult)
    {
        // Step 1: Authenticate with Xbox Live (ProjBobcat STAGE 1)
        var accessToken = msResult.AccessToken;
        var xblRequest = new
        {
            Properties = new
            {
                AuthMethod = "RPS",
                SiteName = "user.auth.xboxlive.com",
                RpsTicket = $"d={accessToken}"
            },
            RelyingParty = "http://auth.xboxlive.com",
            TokenType = "JWT"
        };

        var xblResponse = await HttpClient.PostAsync(
            XBOX_LIVE_AUTH_URL,
            new StringContent(JsonSerializer.Serialize(xblRequest), Encoding.UTF8, "application/json"));

        if (!xblResponse.IsSuccessStatusCode)
        {
            DebugLogger.Log($"Xbox Live auth failed: {await xblResponse.Content.ReadAsStringAsync()}");
            throw new Exception("Failed to authenticate with Xbox Live");
        }

        var xblJson = JsonSerializer.Deserialize<JsonElement>(await xblResponse.Content.ReadAsStringAsync());
        var xblToken = xblJson.GetProperty("Token").GetString();
        DebugLogger.Log($"Xbox Live token acquired: {xblToken.Substring(0, 20)}...");

        // Step 2: Get XSTS token for Xbox Live to fetch xuid (ProjBobcat STAGE 2.5)
        var xstsXuidRequest = new
        {
            Properties = new
            {
                SandboxId = "RETAIL",
                UserTokens = new[] { xblToken }
            },
            RelyingParty = "http://xboxlive.com",
            TokenType = "JWT"
        };

        var xstsXuidResponse = await HttpClient.PostAsync(
            XSTS_AUTH_URL,
            new StringContent(JsonSerializer.Serialize(xstsXuidRequest), Encoding.UTF8, "application/json"));

        var xuid = Guid.Empty.ToString("N");

        if (!xstsXuidResponse.IsSuccessStatusCode)
        {
            DebugLogger.Log($"XSTS auth for xuid failed: {await xstsXuidResponse.Content.ReadAsStringAsync()}");
            throw new Exception("Failed to get XSTS token for xuid");
        }
        
        var xUidRes =
            await xstsXuidResponse.Content.ReadFromJsonAsync(AuthXSTSResponseModelContext.Default.AuthXSTSResponseModel);

        if (xUidRes != null)
        {
            var isXUidXUiExists = xUidRes.DisplayClaims.TryGetProperty("xui", out var xuidXui);
            JsonElement? firstXuidXui = isXUidXUiExists ? xuidXui[0] : null;

            if (firstXuidXui.HasValue && firstXuidXui.Value.TryGetProperty("xid", out var xid) &&
                xid.ValueKind == JsonValueKind.String &&
                !string.IsNullOrEmpty(xid.GetString())) xuid = xid.GetString()!;
        }
        DebugLogger.Log($"Extracted xuid: {xuid}");

        // Step 3: Get XSTS token for Minecraft (ProjBobcat STAGE 2)
        var xstsMinecraftRequest = new
        {
            Properties = new
            {
                SandboxId = "RETAIL",
                UserTokens = new[] { xblToken }
            },
            RelyingParty = "rp://api.minecraftservices.com/",
            TokenType = "JWT"
        };

        var xstsMinecraftResponse = await HttpClient.PostAsync(
            XSTS_AUTH_URL,
            new StringContent(JsonSerializer.Serialize(xstsMinecraftRequest), Encoding.UTF8, "application/json"));

        if (!xstsMinecraftResponse.IsSuccessStatusCode)
        {
            var errorContent = await xstsMinecraftResponse.Content.ReadAsStringAsync();
            var errJson = JsonSerializer.Deserialize<JsonElement>(errorContent);
            var xErr = errJson.TryGetProperty("XErr", out var xErrProp) ? xErrProp.GetInt64() : 0;
            var reason = xErr switch
            {
                2148916233 => "未创建 XBox 账户",
                2148916238 => "未成年人账户",
                _ => "未知"
            };
            DebugLogger.Log($"XSTS auth for Minecraft failed: {reason}, {errorContent}");
            throw new Exception($"XSTS auth failed: {reason}");
        }

        var xstsMinecraftJson = JsonSerializer.Deserialize<JsonElement>(await xstsMinecraftResponse.Content.ReadAsStringAsync());
        var xstsMinecraftToken = xstsMinecraftJson.GetProperty("Token").GetString();

        // Extract userHash (uhs) from xstsMinecraft (ProjBobcat STAGE 3)
        var userHash = await ExtractUserHash(xstsMinecraftJson);
        DebugLogger.Log($"Extracted userHash: {userHash}");

        // Step 4: Authenticate with Minecraft (ProjBobcat STAGE 3)
        var mcRequest = new { identityToken = $"XBL3.0 x={userHash};{xstsMinecraftToken}" };
        var mcResponse = await HttpClient.PostAsync(
            MOJANG_AUTH_URL,
            new StringContent(JsonSerializer.Serialize(mcRequest), Encoding.UTF8, "application/json"));

        if (!mcResponse.IsSuccessStatusCode)
        {
            DebugLogger.Log($"Minecraft auth failed: {await mcResponse.Content.ReadAsStringAsync()}");
            throw new Exception("Failed to authenticate with Minecraft");
        }

        var mcJson = JsonSerializer.Deserialize<JsonElement>(await mcResponse.Content.ReadAsStringAsync());
        var mcToken = mcJson.GetProperty("access_token").GetString();
        DebugLogger.Log("Minecraft token acquired");

        // Step 5: Check game ownership (ProjBobcat STAGE 4)
        HttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", mcToken);
        var ownershipResponse = await HttpClient.GetAsync(MOJANG_OWNERSHIP_URL);
        if (!ownershipResponse.IsSuccessStatusCode)
        {
            DebugLogger.Log($"Ownership check failed: {await ownershipResponse.Content.ReadAsStringAsync()}");
            throw new Exception("Failed to check game ownership");
        }

        var ownershipJson = JsonSerializer.Deserialize<JsonElement>(await ownershipResponse.Content.ReadAsStringAsync());
        if (!ownershipJson.TryGetProperty("items", out var items) || items.EnumerateArray().Count() == 0)
        {
            DebugLogger.Log("No Minecraft ownership found");
            throw new Exception("Account does not own Minecraft");
        }

        // Step 6: Get Minecraft profile (ProjBobcat STAGE 5)
        var profileResponse = await HttpClient.GetAsync(MOJANG_PROFILE_URL);
        if (!profileResponse.IsSuccessStatusCode)
        {
            DebugLogger.Log($"Profile fetch failed: {await profileResponse.Content.ReadAsStringAsync()}");
            throw new Exception("Failed to fetch Minecraft profile");
        }

        var profileJson = JsonSerializer.Deserialize<JsonElement>(await profileResponse.Content.ReadAsStringAsync());
        var username = profileJson.GetProperty("name").GetString();
        var uuid = profileJson.GetProperty("id").GetString();
        DebugLogger.Log($"Minecraft profile fetched: {username}, {uuid}");

        HttpClient.DefaultRequestHeaders.Authorization = null; // Clear header

        DebugLogger.Log("Minecraft authentication successful");
        return (userHash, mcToken, xuid, username, uuid);
    }

    private static async Task<string> ExtractXuid(string xstsToken)
    {
        var payloadBase64 = xstsToken.Split('.')[1];
        var paddedBase64 = payloadBase64.PadRight(4 * ((payloadBase64.Length + 3) / 4), '=');
        var decodedBytes = Convert.FromBase64String(paddedBase64.Replace('-', '+').Replace('_', '/'));
        var decodedString = Encoding.UTF8.GetString(decodedBytes);

        DebugLogger.Log($"Decoded xsts payload for xuid: {decodedString}");
        Trace.WriteLine(decodedString);

        var payload = JsonSerializer.Deserialize<JsonElement>(decodedString);
        if (!payload.TryGetProperty("DisplayClaims", out var displayClaims) ||
            !displayClaims.TryGetProperty("xui", out var xuiArray) ||
            xuiArray.ValueKind != JsonValueKind.Array ||
            !xuiArray.EnumerateArray().Any())
        {
            throw new Exception("Failed to extract DisplayClaims.xui for xuid");
        }

        var firstXui = xuiArray.EnumerateArray().First();
        if (!firstXui.TryGetProperty("xid", out var xid) || xid.ValueKind != JsonValueKind.String)
            throw new Exception("Failed to extract xid from xsts token");

        var xuid = xid.GetString();
        if (string.IsNullOrEmpty(xuid))
            throw new Exception("Extracted xuid is empty");

        return xuid;
    }

    private static async Task<string> ExtractUserHash(JsonElement xstsJson)
    {
        if (!xstsJson.TryGetProperty("DisplayClaims", out var displayClaims) ||
            !displayClaims.TryGetProperty("xui", out var xuiArray) ||
            xuiArray.ValueKind != JsonValueKind.Array ||
            !xuiArray.EnumerateArray().Any())
        {
            throw new Exception("Failed to extract DisplayClaims.xui for userHash");
        }

        var firstXui = xuiArray.EnumerateArray().First();
        if (!firstXui.TryGetProperty("uhs", out var uhs) || uhs.ValueKind != JsonValueKind.String)
            throw new Exception("Failed to extract uhs from xsts token");

        var userHash = uhs.GetString();
        if (string.IsNullOrEmpty(userHash))
            throw new Exception("Extracted userHash is empty");

        return userHash;
    }
}