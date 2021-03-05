using System.ComponentModel;

namespace ME2Randomizer.Classes.Telemetry
{
    /// <summary>
    /// Class used to provide api key access. The keys must be defined in an additional partial file to this one
    /// </summary>
    [Localizable(false)]
    public static partial class APIKeys
    {
        public static bool HasAppCenterKey => typeof(ME2Randomizer.Classes.Telemetry.APIKeys).GetProperty("Private_AppCenter") != null;
        public static string AppCenterKey => (string)typeof(ME2Randomizer.Classes.Telemetry.APIKeys).GetProperty("Private_AppCenter")?.GetValue(typeof(ME2Randomizer.Classes.Telemetry.APIKeys));
    }
}
