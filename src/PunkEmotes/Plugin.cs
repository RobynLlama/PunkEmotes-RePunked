using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using PunkEmotes.Patches;

namespace PunkEmotes;

[BepInPlugin("punkalyn.punkemotes", "PunkEmotes", LCMPluginInfo.PLUGIN_VERSION)]
[BepInProcess("ATLYSS.exe")]

public class PunkEmotesPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        Log.Logger = base.Logger;
        Log.LogInfo("Punk Emotes is rockin'!");

        var harmony = new Harmony("punkalyn.punkemotes");
        int expectedPatches = 5;
        try
        {
            //Explicitly patch our types here
            harmony.PatchAll(typeof(CharacterSelectManager_Patches));
            harmony.PatchAll(typeof(ChatBehaviour_Patches));
            harmony.PatchAll(typeof(Player_Patches));
            harmony.PatchAll(typeof(PlayerMove_Patches));
            harmony.PatchAll(typeof(PlayerVisual_Patches));

            if (expectedPatches != harmony.GetPatchedMethods().Count())
            {
                Log.LogError($"Punk Emotes patched {harmony.GetPatchedMethods().Count()} methods out of {expectedPatches} intended patches!");
            }
        }
        catch (Exception ex)
        {
            Log.LogError($"Exception caught while patching: {ex.Message}");
        }
        InitializeConfigs();
    }

    public class Log
    {
        // BepInEx ManualLogSource instance, now named 'Log' for simplicity
        internal static ManualLogSource Logger;

        // Track whether logging is enabled for the current scope (e.g., method)
        public static bool logInfoEnabled = true;
        public static bool logWarningEnabled = true;
        public static bool logErrorEnabled = true;
        private static bool logMethod = true;

        public static void LogMethod(bool enable)
        {
            logMethod = enable;
        }

        // Log info messages based on the LogInfoEnabled flag or the optional shouldLog argument
        public static void LogInfo(string message, bool? shouldLog = null)
        {
            // If shouldLog is provided, use it, otherwise respect the global setting
            if ((shouldLog ?? logInfoEnabled) && logMethod)
            {
                Logger.LogInfo(message);
            }
        }

        // Log warning messages based on the LogWarningEnabled flag or the optional shouldLog argument
        public static void LogWarning(string message, bool? shouldLog = null)
        {
            // If shouldLog is provided, use it, otherwise respect the global setting
            if ((shouldLog ?? logWarningEnabled) && logMethod)
            {
                Logger.LogWarning(message);
            }
        }

        // Log error messages based on the LogErrorEnabled flag or the optional shouldLog argument
        public static void LogError(string message, bool? shouldLog = null)
        {
            // If shouldLog is provided, use it, otherwise respect the global setting
            if ((shouldLog ?? logErrorEnabled) && logMethod)
            {
                Logger.LogError(message);
            }
        }
    }

    internal static ConfigEntry<string> playerOverridesJson;

    private void InitializeConfigs()
    {
        playerOverridesJson = Config.Bind("Overrides", "playerOverrides", "{}",
            "Serialized dictionary of player overrides in JSON format.");
    }

    public static void SaveOverrides(Dictionary<string, string> playerOverrides, string animationOverride, string targetOverride, bool delete = false)
    {
        if (delete)
        {
            // Remove the entry from the dictionary
            playerOverrides.Remove(targetOverride);

            // Serialize the dictionary back into a JSON string
            string json = JsonConvert.SerializeObject(playerOverrides, Formatting.Indented);

            // Save the updated JSON string back to the config
            playerOverridesJson.Value = json;
        }
        else
        {

            // Apply the override to the dictionary
            playerOverrides[targetOverride] = animationOverride;

            // Serialize the dictionary back into a JSON string
            string json = JsonConvert.SerializeObject(playerOverrides, Formatting.Indented);

            // Save the updated JSON string back to the config
            playerOverridesJson.Value = json;
        }
    }
}
