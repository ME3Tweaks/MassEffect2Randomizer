using ME3TweaksCore.Helpers;

namespace RandomizerUI.Classes
{
    public enum ESetting
    {
        /// <summary>
        /// Set when the firstrun flyout has completed
        /// </summary>
        SETTING_FIRSTRUN
    }

    class MERSettings
    {
        public const string RegistryKeyPath = @"HKEY_CURRENT_USER\Software\MassEffectRandomizer";
#if __GAME1__
        private const string SettingPrefix = "ME1-";
#elif __GAME2__
        private const string SettingPrefix = "ME2-";
#elif __GAME3__
        private const string SettingPrefix = "ME3-";
#endif

        /// <summary>
        /// Creates the key for settings
        /// </summary>
        public static void InitRegistryKey()
        {
            RegistryHandler.CreateRegistryPath(RegistryKeyPath);
        }

        public static void WriteSettingString(ESetting setting, string data)
        {
            RegistryHandler.WriteRegistryString(RegistryKeyPath, $"{SettingPrefix}{setting}", data);
        }

        public static void WriteSettingBool(ESetting setting, bool data)
        {
            RegistryHandler.WriteRegistryBool(RegistryKeyPath, $"{SettingPrefix}{setting}", data);
        }

        public static string GetSettingString(ESetting setting)
        {
            return RegistryHandler.GetRegistryString(RegistryKeyPath, $"{SettingPrefix}{setting}");
        }

        public static bool GetSettingBool(ESetting setting)
        {
            return RegistryHandler.GetRegistryBool(RegistryKeyPath, $"{SettingPrefix}{setting}");
        }

        public static void DeleteSetting(ESetting setting)
        {
            RegistryHandler.DeleteRegistryKey(RegistryKeyPath, $"{SettingPrefix}{setting}");
        }
    }
}
