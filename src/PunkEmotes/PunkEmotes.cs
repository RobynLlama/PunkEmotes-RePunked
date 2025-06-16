using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Mirror;
using Newtonsoft.Json;
using PunkEmotes.Components;
using PunkEmotes.Internals;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

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
            harmony.PatchAll();
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

    internal static void SendLocalMessage(string message)
    {
        if (!Player._mainPlayer) return;
        Player._mainPlayer._cB.New_ChatMessage(message);
    }

    [HarmonyPatch]
    public static class PunkEmotesPatchWrapper
    {
        // Replace chat-based patches with direct RPC calls for animations and overrides

        // Patch for playing emotes - Triggered when an emote is played, either from command or remote player request
        [HarmonyPatch(typeof(ChatBehaviour), "Send_ChatMessage")]
        static bool Prefix(ref string _message, ChatBehaviour __instance)
        {
            if (string.IsNullOrEmpty(_message)) return true;

            // Check for the "/em " command
            if (!_message.StartsWith("/em ", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (Player._mainPlayer == null)
            {
                SendLocalMessage("Main player is null! This is a really bad sign!");
                return true; // Allow default handling if something is wrong
            }

            if (AnimationConstructor.PunkEmotesLibrary.Instance == null)
            {
                SendLocalMessage("PunkEmotesLibrary instance is null!");
                return true; // Avoid further execution if the library is unavailable
            }

            // Use PunkEmotesManager to handle animations locally
            PunkEmotesManager emotesManager = Player._mainPlayer.GetComponent<PunkEmotesManager>();
            if (emotesManager == null)
            {
                SendLocalMessage("EmotesManager not found for player!");
                return true; // Allow default chat behavior if emotesManager is missing
            }

            string trimmedCommand = _message.Substring(4).Trim(); // Do not convert to lowercase
            string[] parts = trimmedCommand.Split(' ');

            switch (parts[0])
            {
                case string command when string.Equals(command, "overrides", StringComparison.OrdinalIgnoreCase):
                    if (parts.Length == 1 || string.IsNullOrWhiteSpace(parts[1]))
                    {
                        // List available override animations
                        emotesManager.ListAvailableOverrides();
                        _message = string.Empty;
                        return false;
                    }
                    else
                    {
                        SendLocalMessage("'/em overrides' is used to list override targets, use '/em override animation target' if this was a mistake");
                        _message = string.Empty;
                    }
                    return false;

                case string command when string.Equals(command, "help", StringComparison.OrdinalIgnoreCase):
                    if (parts.Length == 1 || string.IsNullOrWhiteSpace(parts[1]))
                    {
                        // Show help messages
                        SendLocalMessage("For information on emotes, use '/em help emotes'");
                        SendLocalMessage("For information on overrides, use '/em help overrides'");
                        _message = string.Empty;
                        return false;
                    }
                    else if (string.Equals(parts[1], "emotes", StringComparison.OrdinalIgnoreCase))
                    {
                        SendLocalMessage("Use '/em [category] animation_name' to play an animation.");
                        SendLocalMessage("[category] is an optional sorting tag. For example, '/em sit imp' uses the first 'imp' animation in the 'sit' category.");
                        _message = string.Empty;
                        return false;
                    }
                    else if (string.Equals(parts[1], "overrides", StringComparison.OrdinalIgnoreCase))
                    {
                        SendLocalMessage("Use '/em override animation_name target_name' to apply overrides.");
                        SendLocalMessage("animation_name is the name of the animation you want to use.");
                        SendLocalMessage("target_name is the name of the animation you want to replace.");
                        SendLocalMessage("To get a list of targets, use '/em overrides', but it's a big list! It'll clear your chat.");
                        SendLocalMessage("To undo an override, use '/em override delete target', or '/em override delete all' to reset to default.");
                        _message = string.Empty;
                        return false;
                    }
                    else
                    {
                        SendLocalMessage($"Unrecognized help topic: {parts[1]}");
                        _message = string.Empty;
                        return false;
                    }

                case string command when string.Equals(command, "override", StringComparison.OrdinalIgnoreCase):
                    if (parts.Length == 3)
                    {
                        // Check if the command is for deleting an override
                        if (string.Equals(parts[1], "delete", StringComparison.OrdinalIgnoreCase))
                        {
                            emotesManager.RemoveOverride(emotesManager, parts[2]);
                            // Notify the network of the override removal
                            PunkEmotesNetwork.Instance.Cmd_ClearOverrides(emotesManager, parts[2]);
                        }
                        else
                        {
                            // Determine the target name based on alias lookup or user input
                            string targetName = emotesManager.overrideAliases.ContainsKey(parts[1].ToLowerInvariant())
                                ? emotesManager.overrideAliases[parts[1].ToLowerInvariant()]
                                : parts[1];

                            // Handle specific cases for "sit" and "sit2"
                            if (string.Equals(parts[1], "sit", StringComparison.OrdinalIgnoreCase))
                            {
                                string clipSitInit = AnimationConstructor.PunkEmotesLibrary.Instance.GetAnimation(parts[2], "override_playerEmote_sitInit").name;
                                string clipSitLoop = AnimationConstructor.PunkEmotesLibrary.Instance.GetAnimation(parts[2], "override_playerEmote_sitLoop").name;

                                emotesManager.ApplyPunkOverrides(emotesManager, clipSitInit, "_playerEmote_sitInit");
                                emotesManager.ApplyPunkOverrides(emotesManager, clipSitLoop, "_playerEmote_sitLoop");

                                PunkEmotesNetwork.Instance.Cmd_OverrideChange(emotesManager, clipSitInit, "_playerEmote_sitInit");
                                PunkEmotesNetwork.Instance.Cmd_OverrideChange(emotesManager, clipSitLoop, "_playerEmote_sitLoop");
                            }
                            else if (string.Equals(parts[2], "sit2", StringComparison.OrdinalIgnoreCase))
                            {
                                string clipSitInit = AnimationConstructor.PunkEmotesLibrary.Instance.GetAnimation(parts[2], "override_playerEmote_sitInit02").name;
                                string clipSitLoop = AnimationConstructor.PunkEmotesLibrary.Instance.GetAnimation(parts[2], "override_playerEmote_sitLoop02").name;

                                emotesManager.ApplyPunkOverrides(emotesManager, clipSitInit, "_playerEmote_sitInit02");
                                emotesManager.ApplyPunkOverrides(emotesManager, clipSitLoop, "_playerEmote_sitLoop02");

                                PunkEmotesNetwork.Instance.Cmd_OverrideChange(emotesManager, clipSitInit, "_playerEmote_sitInit02");
                                PunkEmotesNetwork.Instance.Cmd_OverrideChange(emotesManager, clipSitLoop, "_playerEmote_sitLoop02");
                            }
                            else if (AnimationConstructor.PunkEmotesLibrary.Instance.NormalizeAnimationName(parts[2], "override" + targetName) != null)
                            {
                                {
                                    // Handle general override cases using the resolved targetName
                                    string clip = AnimationConstructor.PunkEmotesLibrary.Instance.GetAnimation(parts[2], "override" + targetName).name;

                                    emotesManager.ApplyPunkOverrides(emotesManager, clip, targetName);
                                    PunkEmotesNetwork.Instance.Cmd_OverrideChange(emotesManager, clip, targetName);
                                }
                            }
                            else
                            {
                                // Animation not found
                                SendLocalMessage($"Animation {parts[2]} not found as {targetName} override!");
                            }
                        }
                    }
                    else
                    {
                        // Invalid format
                        SendLocalMessage("Invalid override command format!");
                    }

                    _message = string.Empty;
                    return false;

                default:
                    // Handle animations with category and name
                    if (parts.Length == 2 && AnimationConstructor.PunkEmotesLibrary.Instance.NormalizeAnimationName(parts[1], parts[0]) != null)
                    {
                        string clip = AnimationConstructor.PunkEmotesLibrary.Instance.GetAnimation(parts[1], parts[0]).name;
                        emotesManager.PlayAnimationClip(emotesManager, clip, parts[0]);
                        PunkEmotesNetwork.Instance.Cmd_AnimationChange(emotesManager, clip, parts[0]);
                    }
                    else if (parts.Length == 1 && AnimationConstructor.PunkEmotesLibrary.Instance.NormalizeAnimationName(parts[0]) != null)
                    {
                        string clip = AnimationConstructor.PunkEmotesLibrary.Instance.GetAnimation(parts[0]).name;
                        emotesManager.PlayAnimationClip(emotesManager, clip);
                        PunkEmotesNetwork.Instance.Cmd_AnimationChange(emotesManager, clip);
                    }
                    else
                    {
                        SendLocalMessage($"Animation {string.Join(' ', parts)} not found!");
                    }
                    _message = string.Empty;
                    return false;
            }
        }

        [HarmonyPatch(typeof(CharacterSelectManager), "Select_CharacterFile")]
        class ResetCache
        {
            static void Postfix()
            {
                AnimationConstructor.raceAnimatorReset = true;
                PlayerRegistry.ClearRegistry();
                //LogInfo("Reset call for animators");
            }
        }

        [HarmonyPatch(typeof(PlayerVisual), "Iterate_AnimationCallback")]
        public class LoadFBX
        {
            static void Postfix(PlayerVisual __instance, ref string _animName, ref float _animLayer)
            {
                if (AnimationConstructor.raceAnimatorReset == true)
                {
                    AnimationConstructor.LoadRaceFBXs();
                }
            }
        }

        // Patch for stopping animation when the player moves
        [HarmonyPatch(typeof(PlayerMove), "Set_MovementAction")]
        public class SetMovementActionPatch
        {
            static void Prefix(PlayerMove __instance, MovementAction _mA)
            {
                var player = __instance.gameObject.GetComponent<Player>();
                if (player == null) return;

                var emotesManager = player.GetComponent<PunkEmotesManager>();
                if (emotesManager == null) return;

                if (_mA != MovementAction.IDLE && emotesManager._isAnimationPlaying)
                {
                    // Stop the animation if moving
                    emotesManager.StopAnimation(emotesManager);
                }
            }
        }

        // Attach PunkEmotesManager to player and register them when they spawn
        [HarmonyPatch(typeof(Player), "OnStartAuthority")]
        static void Postfix(Player __instance)
        {
            var emotesManager = __instance.gameObject.GetComponent<PunkEmotesManager>();
            if (emotesManager == null)
            {
                // Attach the PunkEmotesManager if not already attached
                emotesManager = __instance.gameObject.AddComponent<PunkEmotesManager>();
            }

            var punkEmotesNetwork = Player._mainPlayer.GetComponent<PunkEmotesNetwork>();
            if (punkEmotesNetwork == null)
            {
                punkEmotesNetwork = __instance.gameObject.AddComponent<PunkEmotesNetwork>();
            }

            if (__instance._isHostPlayer)
            {
                // Register player with the PlayerRegistry
                PlayerRegistry.RegisterPlayer(__instance.netId, __instance._nickname, __instance, emotesManager);
            }
        }
    }
}
