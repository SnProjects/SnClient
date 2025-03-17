using System.IO.Compression;
using Newtonsoft.Json;
using ProjBobcat.Class.Helper;
using SnClient.Utils;

namespace SnClient.GameBasis;

public static class JavaDetection
{
    private const string API_URL =
        "https://api.adoptium.net/v3/assets/latest/{0}/hotspot?architecture={1}&image_type=jre&os={2}&vendor=eclipse";

    public static async Task<string> GetJava(int majorVersion)
    {
        var os = OSHelper.GetOS();
        var arch = OSHelper.GetArch();
        
        var packagePath = Path.Combine(Core.rootPath, $"jre\\jre-{majorVersion}");
        if (Directory.Exists(packagePath))
        {
            DebugLogger.Log("Java already downloaded.");
            
            // get the inner folder
            var innerFolder = Directory.GetDirectories(packagePath)[0];
            
            return Path.Combine(innerFolder, "bin\\javaw.exe");
        }
        
        DebugLogger.Log("Java not found, Checking for Java updates...");
        
        var url = string.Format(API_URL, majorVersion, arch, os);
        
        // fetch data from API
        var response = await HttpHelper.Get(url);
        var data = await response.Content.ReadAsStringAsync();
        
        // parse json
        var javaInfo = JsonConvert.DeserializeObject<JavaInfo[]>(data);
        
        if (javaInfo == null)
        {
            DebugLogger.Log("Failed to get Java info.");
            return "";
        }
        
        // download java
        return await DownloadJava(javaInfo[0], majorVersion);
    }
    
    private static async Task<string> DownloadJava(JavaInfo javaInfo, int majorVersion)
    {
        DebugLogger.Log("Downloading Java...");
        
        var package = javaInfo.Binary.Package;
        
        // download package
        var packageRes = await HttpHelper.Get(package.Link);
        var packageData = await packageRes.Content.ReadAsByteArrayAsync();
        
        // save package
        var path = Path.Combine(Core.rootPath, $"jre\\jre-{majorVersion}");
        var packagePath = Path.Combine(path, package.Name);
        
        // create directory
        Directory.CreateDirectory(path);
        
        await File.WriteAllBytesAsync(packagePath, packageData);
        
        // extract package
        ZipFile.ExtractToDirectory(packagePath, path);
        
        // rename the inner folder to the checksum
        var innerFolder = Directory.GetDirectories(path)[0];
        var checksum = package.Checksum;
        var newPath = Path.Combine(path, checksum);
        Directory.Move(innerFolder, newPath);
        
        // delete package
        File.Delete(packagePath);
        
        DebugLogger.Log("Java downloaded.");
        
        return path + "\\bin\\javaw.exe";
    }
    
    /* example JavaInfo data
     [
    {
        "binary": {
            "architecture": "x64",
            "download_count": 702170,
            "heap_size": "normal",
            "image_type": "jre",
            "installer": {
                "checksum": "3f511d4edbb81fdb7d044cabede018b0823b2f277103f5f47e8c72b526e9c256",
                "checksum_link": "https://github.com/adoptium/temurin21-binaries/releases/download/jdk-21.0.6%2B7/OpenJDK21U-jre_x64_windows_hotspot_21.0.6_7.msi.sha256.txt",
                "download_count": 225381,
                "link": "https://github.com/adoptium/temurin21-binaries/releases/download/jdk-21.0.6%2B7/OpenJDK21U-jre_x64_windows_hotspot_21.0.6_7.msi",
                "metadata_link": "https://github.com/adoptium/temurin21-binaries/releases/download/jdk-21.0.6%2B7/OpenJDK21U-jre_x64_windows_hotspot_21.0.6_7.msi.json",
                "name": "OpenJDK21U-jre_x64_windows_hotspot_21.0.6_7.msi",
                "signature_link": "https://github.com/adoptium/temurin21-binaries/releases/download/jdk-21.0.6%2B7/OpenJDK21U-jre_x64_windows_hotspot_21.0.6_7.msi.sig",
                "size": 34988032
            },
            "jvm_impl": "hotspot",
            "os": "windows",
            "package": {
                "checksum": "707c981a4ff9e680a9ea5d6f625eafe8bc47e1f89140a67d761fde24fc02ab49",
                "checksum_link": "https://github.com/adoptium/temurin21-binaries/releases/download/jdk-21.0.6%2B7/OpenJDK21U-jre_x64_windows_hotspot_21.0.6_7.zip.sha256.txt",
                "download_count": 476789,
                "link": "https://github.com/adoptium/temurin21-binaries/releases/download/jdk-21.0.6%2B7/OpenJDK21U-jre_x64_windows_hotspot_21.0.6_7.zip",
                "metadata_link": "https://github.com/adoptium/temurin21-binaries/releases/download/jdk-21.0.6%2B7/OpenJDK21U-jre_x64_windows_hotspot_21.0.6_7.zip.json",
                "name": "OpenJDK21U-jre_x64_windows_hotspot_21.0.6_7.zip",
                "signature_link": "https://github.com/adoptium/temurin21-binaries/releases/download/jdk-21.0.6%2B7/OpenJDK21U-jre_x64_windows_hotspot_21.0.6_7.zip.sig",
                "size": 48865456
            },
            "project": "jdk",
            "scm_ref": "jdk-21.0.6+7_adopt",
            "updated_at": "2025-01-23T15:25:40Z"
        },
        "release_link": "https://github.com/adoptium/temurin21-binaries/releases/tag/jdk-21.0.6%2B7",
        "release_name": "jdk-21.0.6+7",
        "vendor": "eclipse",
        "version": {
            "build": 7,
            "major": 21,
            "minor": 0,
            "openjdk_version": "21.0.6+7-LTS",
            "optional": "LTS",
            "security": 6,
            "semver": "21.0.6+7.0.LTS"
        }
    }
]*/
    public class JavaInfo
    {
        public Binary Binary { get; set; }
        public string ReleaseLink { get; set; }
        public string ReleaseName { get; set; }
        public string Vendor { get; set; }
        public Version Version { get; set; }
    }
    
    public class Binary
    {
        public string Architecture { get; set; }
        public int DownloadCount { get; set; }
        public string HeapSize { get; set; }
        public string ImageType { get; set; }
        public Installer Installer { get; set; }
        public string JvmImpl { get; set; }
        public string Os { get; set; }
        public Package Package { get; set; }
        public string Project { get; set; }
        public string ScmRef { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
    
    public class Installer
    {
        public string Checksum { get; set; }
        public string ChecksumLink { get; set; }
        public int DownloadCount { get; set; }
        public string Link { get; set; }
        public string MetadataLink { get; set; }
        public string Name { get; set; }
        public string SignatureLink { get; set; }
        public int Size { get; set; }
    }
    
    public class Package
    {
        public string Checksum { get; set; }
        public string ChecksumLink { get; set; }
        public int DownloadCount { get; set; }
        public string Link { get; set; }
        public string MetadataLink { get; set; }
        public string Name { get; set; }
        public string SignatureLink { get; set; }
        public int Size { get; set; }
    }
    
    public class Version
    {
        public int Build { get; set; }
        public int Major { get; set; }
        public int Minor { get; set; }
        public string OpenjdkVersion { get; set; }
        public string Optional { get; set; }
        public int Security { get; set; }
        public string Semver { get; set; }
    }
}