using ProjBobcat.Class.Helper;

namespace SnClient.GameBasis;

public static class JavaDetection
{
    public static IEnumerable<string> DetectJava()
    {
        // Returns a list of all java installations found in registry.
        return SystemInfoHelper.FindJava();
    }
}