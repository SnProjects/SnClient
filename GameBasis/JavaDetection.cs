using ProjBobcat.Class.Helper;

namespace SnClient.GameBasis;

public static class JavaDetection
{
    public static async Task<List<string>> DetectJava()
    {
        // Returns a list of all java installations found in registry.
        var javas = ProjBobcat.Class.Helper.SystemInfoHelper.FindJava(); // Returns a list of all Java installations found in the registry.
        List<string> javaList = new List<string>();
        
        await foreach (var java in javas)
        {
            javaList.Add(java);
        }
        
        return javaList;
    }
}