using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace TSARBOMBA
{
    public static class ModInfo
    {
        public const string GUID = "example.assassin1076.TSARBOMBA_migration";
        public const string Name = "TSARBOMBA";
        public const string Version = "0.0.1";
    }

    [BepInPlugin(ModInfo.GUID, ModInfo.Name, ModInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;

        private void Awake()
        {
            new Harmony(ModInfo.GUID).PatchAll();
            // Plugin startup logic
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {ModInfo.GUID} is loaded!");
        }
    }
}
