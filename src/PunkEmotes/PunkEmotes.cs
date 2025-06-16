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
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using static PunkEmotes.PunkEmotesPlugin.AnimationConstructor;

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

    private static ConfigEntry<string> playerOverridesJson;

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

    public static void CheckModVersion(string version)
    {
        if (string.IsNullOrEmpty(version))
        {
            SendLocalMessage("Server's PunkEmotes version not detected, but plugin appears to be installed.");
        }
        else if (version != LCMPluginInfo.PLUGIN_VERSION)
        {
            SendLocalMessage($"PunkEmotes version mismatch: Your version ({LCMPluginInfo.PLUGIN_VERSION}) | Server version: ({version})");
        }
        else
        {
            SendLocalMessage($"PunkEmotes{LCMPluginInfo.PLUGIN_VERSION} detected on server! Have fun <3");
        }
    }

    private static void SendLocalMessage(string message)
    {
        if (!Player._mainPlayer) return;
        Player._mainPlayer._cB.New_ChatMessage(message);
    }

    public class PunkEmotesNetwork : NetworkBehaviour
    {
        // Singleton instance
        public static PunkEmotesNetwork Instance { get; private set; }

        // Ensure only one instance exists and it's accessible
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Log.LogWarning("Found and destroyed duplicate PunkEmotesNetwork");
                Destroy(gameObject);
            }
            else
            {
                Log.LogInfo("Created PunkEmotesNetwork!");
                Instance = this;
                DontDestroyOnLoad(gameObject); // Keeps the instance across scenes
            }
        }

        public bool IsHostPlayer { get; private set; } = false;

        private void Start()
        {
            StartCoroutine(WaitForNetworkContext());
        }
        private IEnumerator WaitForNetworkContext()
        {
            // Wait until both isServer and isClient are set properly
            while (!NetworkServer.active)
            {
                yield return null; // Wait for the next frame
            }

            // Once both are set, determine host status
            IsHostPlayer = (Player._mainPlayer.isServer);
            Log.LogInfo($"Host Status determined after waiting: {IsHostPlayer}");
        }

        private static bool receivedModHandshakeResponse = false;

        // This method will be called to synchronize the animation state when a player joins
        [Command]
        public void Cmd_SyncRequest(NetworkConnection conn)
        {
            if (conn == null)
            {
                Log.LogError("CmdSyncRequest: connectionToClient is null!");
                return;
            }

            if (IsHostPlayer)
            {
                Log.LogInfo($"CmdSyncRequest received from connection: {conn}");
                Rpc_HandshakeResponse(conn, LCMPluginInfo.PLUGIN_VERSION);
                Rpc_SyncAnimationResponse(conn);
            }

            // Start a timeout check to see if the mod is installed and the server is responsive to our calls
            StartCoroutine(WaitForModHandshakeResponse(conn));
        }

        private IEnumerator WaitForModHandshakeResponse(NetworkConnection conn)
        {
            float timeoutDuration = 5.0f; // Timeout duration in seconds
            float timeElapsed = 0f;

            // Wait until the timeout duration or until we get the response
            while (timeElapsed < timeoutDuration)
            {
                if (receivedModHandshakeResponse)  // Flag to indicate if we received the response
                {
                    yield break;  // Exit the coroutine if the response is received
                }

                timeElapsed += Time.deltaTime;
                yield return null;  // Wait for the next frame
            }

            // Timeout reached without a response
            SendLocalMessage("Server did not respond or does not have PunkEmotes installed. Custom emotes will likely not be broadcasted.");
        }

        // This method will send the animation sync response back to the requesting player
        [TargetRpc]
        public void Rpc_SyncAnimationResponse(NetworkConnection conn)
        {
            if (!IsHostPlayer)
            {
                Log.LogInfo($"SyncAnimationResponse detected!");
                // Iterate through all players in the PlayerRegistry, excluding the Local player
                foreach (var playerEntry in PlayerRegistry._playersByNetId.Values)
                {
                    // Skip the Local player (the one who sent the sync request)
                    if (playerEntry.PlayerInstance.netId != conn.identity.netId)
                    {
                        // Send the animation data for this player (Remote) to the Local player
                        SendPlayerAnimationData(playerEntry.EmotesManager);

                        // Set the response flag to true so that we don't get a timeout message
                        receivedModHandshakeResponse = true;
                    }
                }
            }
        }

        [TargetRpc]
        public void Rpc_HandshakeResponse(NetworkConnection conn, string version)
        {
            if (IsHostPlayer)
            {
                Log.LogInfo($"Sending Handshake Response to {conn} with version {version}");
                CheckModVersion(version);
            }
        }

        private void SendPlayerAnimationData(PunkEmotesManager remoteEmotesManager)
        {
            Log.LogInfo($"Sending Player Animation Data to {remoteEmotesManager._player._nickname}");
            // Get the relevant animation data for this player's PunkEmotesManager
            remoteEmotesManager.GetPlayerAnimationState(remoteEmotesManager);  // Retrieve remote player's current animation state
            remoteEmotesManager.GetPlayerAnimationOverrides(remoteEmotesManager);  // Retrieve remote player's current overrides
        }

        // This method will be used to send animation updates to other players (only called by the host)
        [Command]
        public void Cmd_AnimationChange(PunkEmotesManager emotesManager, string animationName, string animationCategory = null)
        {
            if (IsHostPlayer)
            {
                Log.LogInfo($"Client sending animation change command from {emotesManager._player._nickname}: ({animationName}, {animationCategory})");
                // Send animation update request to the host
                Rpc_SendAnimationUpdate(emotesManager, animationName, animationCategory);
            }
        }

        // This method will handle receiving an animation update from another player
        [ClientRpc]
        public void Rpc_SendAnimationUpdate(PunkEmotesManager remoteEmotesManager, string animationName, string animationCategory = null)
        {
            Log.LogInfo($"Server sending animation update response from {remoteEmotesManager._player._nickname}: ({animationName}, {animationCategory})");
            // Apply the received animation update to the local player's emotes manager
            if (!IsHostPlayer)
            {
                remoteEmotesManager.PlayAnimationClip(remoteEmotesManager, animationName, animationCategory);
            }
        }

        [Command]
        public void Cmd_StopAnimation(PunkEmotesManager emotesManager)
        {
            Log.LogInfo($"Client sending stop animation command from {emotesManager._player._nickname}");
            if (IsHostPlayer)
            {
                Rpc_StopAnimation(emotesManager);
            }
        }

        [ClientRpc]
        public void Rpc_StopAnimation(PunkEmotesManager remoteEmotesManager)
        {
            Log.LogInfo($"Server sending stop animation response from {remoteEmotesManager._player._nickname}");
            if (!IsHostPlayer)
            {
                remoteEmotesManager.StopAnimation(remoteEmotesManager);
            }
        }

        // This method will send animation override updates (only called by the host)
        [Command]
        public void Cmd_OverrideChange(PunkEmotesManager emotesManager, string overrideAnimation, string overrideTarget)
        {
            Log.LogInfo($"Client sending override change command from {emotesManager._player._nickname}: ({overrideAnimation}, {overrideTarget})");
            if (IsHostPlayer)
            {
                // Logic to send override updates to all players except the host
                Rpc_SendOverrideUpdate(emotesManager, overrideAnimation, overrideTarget);
            }
        }

        // This method will handle receiving an override update (for clients)
        [ClientRpc]
        public void Rpc_SendOverrideUpdate(PunkEmotesManager remoteEmotesManager, string overrideAnimation, string overrideTarget)
        {
            Log.LogInfo($"Server sending override update from {remoteEmotesManager._player._nickname}");
            // Apply the received override update to the local player's emotes manager
            if (!IsHostPlayer)
            {
                remoteEmotesManager.ApplyPunkOverrides(remoteEmotesManager, overrideAnimation, overrideTarget);
            }
        }

        [Command]
        public void Cmd_ClearOverrides(PunkEmotesManager emotesManager, string overrideTarget)
        {
            Log.LogInfo($"Client sending remove override command from {emotesManager._player._nickname}");
            if (IsHostPlayer)
            {
                Rpc_ClearOverrides(emotesManager, overrideTarget);
            }
        }

        [ClientRpc]
        public void Rpc_ClearOverrides(PunkEmotesManager remoteEmotesManager, string overrideTarget)
        {
            Log.LogInfo($"Server sending remove override response from {remoteEmotesManager._player._nickname}");
            if (!IsHostPlayer)
            {
                remoteEmotesManager.RemoveOverride(remoteEmotesManager, overrideTarget);
            }
        }
    }

    public class AnimationConstructor
    {
        public static Dictionary<string, Animator> raceAnimators = [];
        public static bool raceAnimatorReset = true;

        // Method to load all race-specific FBXs (you can call this once at the start or when a race is loaded)
        public static void LoadRaceFBXs()
        {
            // Assuming you know the list of race names
            string[] raceNames = ["byrdle", "chang", "imp", "Kobold", "poon"];

            foreach (var race in raceNames)
            {
                GameObject raceFBX = GameObject.Find(race + "FBX");
                if (raceFBX != null)
                {
                    Animator raceAnimator = raceFBX.GetComponent<Animator>();
                    raceAnimators[race] = raceAnimator;
                    Log.LogInfo($"{race} loaded into animation memory");
                }
            }
            PunkEmotesLibrary.Instance.PopulateDefaultAnimations();
            raceAnimatorReset = false;
        }

        public class PunkEmotesLibrary
        {
            // Static instance for the Singleton pattern
            private static PunkEmotesLibrary _instance;

            // Dictionary to store animations by their name
            private Dictionary<string, Dictionary<string, AnimationClip>> animationClips =
                new Dictionary<string, Dictionary<string, AnimationClip>>()
                {
                    { "general", new Dictionary<string, AnimationClip>() }, // Uncategorized clips fall in here as a fallback case
                    { "atlyss", new Dictionary<string, AnimationClip>() }, // Atlyss animations are added here so that they're easier to find
                    { "override_playerEmote_sitInit", new Dictionary<string, AnimationClip>() },
                    { "override_playerEmote_sitLoop", new Dictionary<string, AnimationClip>() },
                    { "override_playerEmote_sitInit02", new Dictionary<string, AnimationClip>() },
                    { "override_playerEmote_sitLoop02", new Dictionary<string, AnimationClip>() },
                    { "overrideplayer_idle", new Dictionary<string, AnimationClip>() },
                    { "dance", new Dictionary<string, AnimationClip>() },
                    { "sit", new Dictionary<string, AnimationClip>() },
                };

            // Private constructor to prevent instantiation from outside
            private PunkEmotesLibrary() { }

            // Public static property to access the singleton instance
            public static PunkEmotesLibrary Instance
            {
                get
                {
                    if (_instance == null)
                    {
                        _instance = new PunkEmotesLibrary();
                    }
                    return _instance;
                }
            }

            public void PopulateDefaultAnimations()
            {
                // Loop over each race in the raceAnimators dictionary
                foreach (KeyValuePair<string, Animator> raceAnimatorPair in raceAnimators)
                {
                    string raceName = raceAnimatorPair.Key;
                    Animator raceAnimator = raceAnimatorPair.Value;

                    // Extract animation clips based on specific keywords
                    AnimationClip[] clips = ExtractAnimationsFromAnimator(raceAnimator);

                    // Loop through each clip and check if it contains the desired animation names
                    foreach (AnimationClip clip in clips)
                    {
                        if (clip != null)
                        {
                            if (clip.name.EndsWith("dance"))
                            {
                                if (clip.name == "Kobold_dance")
                                {
                                    clip.name = "kubold_dance";
                                    raceName = "kubold";
                                }
                                // Add dance animation to the library
                                animationClips["atlyss"][$"{raceName}_dance"] = clip;
                                animationClips["dance"][$"{raceName}_dance"] = clip;
                                Log.LogInfo($"Added {clip.name} as {raceName}_dance to animation library!");
                            }

                            if (clip.name.EndsWith("sitInit"))
                            {
                                if (clip.name == "Kobold_sitInit")
                                {
                                    clip.name = "kubold_sitinit";
                                    raceName = "kubold";
                                }

                                animationClips["atlyss"][$"{raceName}_sitinit"] = clip;
                                animationClips["override_playerEmote_sitInit"][$"{raceName}_sitinit"] = clip;
                                animationClips["sit"][$"{raceName}_sitinit"] = clip;
                                Log.LogInfo($"Added {clip.name} as {raceName}_sitinit to animation library!");
                            }



                            if (clip.name.EndsWith("sitLoop"))
                            {
                                if (clip.name == "Kobold_sitLoop")
                                {
                                    clip.name = "kubold_sitloop";
                                    raceName = "kubold";
                                }

                                animationClips["atlyss"][$"{raceName}_sitloop"] = clip;
                                animationClips["override_playerEmote_sitLoop"][$"{raceName}_sitloop"] = clip;
                                animationClips["sit"][$"{raceName}_sitloop"] = clip;
                                Log.LogInfo($"Added {clip.name} as {raceName}_sitloop to animation library!");
                            }



                            if (clip.name.EndsWith("sitInit02"))
                            {
                                if (clip.name == "Kobold_sitInit02")
                                {
                                    clip.name = "kubold_sitinit2";
                                    raceName = "kubold";
                                }

                                animationClips["atlyss"][$"{raceName}_sitinit02"] = clip;
                                animationClips["override_playerEmote_sitInit02"][$"{raceName}_sitinit02"] = clip;
                                animationClips["sit"][$"{raceName}_sitinit02"] = clip;
                                Log.LogInfo($"Added {clip.name} as {raceName}_sitinit02 to animation library!");
                            }



                            if (clip.name.EndsWith("sitLoop02"))
                            {
                                if (clip.name == "Kobold_sitLoop")
                                {
                                    clip.name = "kubold_sitloop02";
                                }

                                animationClips["atlyss"][$"{raceName}_sitloop02"] = clip;
                                animationClips["override_playerEmote_sitLoop02"][$"{raceName}_sitloop02"] = clip;
                                animationClips["sit"][$"{raceName}_sitloop02"] = clip;
                                Log.LogInfo($"Added {clip.name} as {raceName}_sitloop02 to animation library!");
                            }
                        }
                    }
                }
            }

            // Helper method to extract animations from an Animator
            private AnimationClip[] ExtractAnimationsFromAnimator(Animator animator)
            {
                // Retrieve all animation clips from the Animator
                List<AnimationClip> clips = new List<AnimationClip>();

                foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
                {
                    clips.Add(clip);
                }

                return clips.ToArray();
            }

            // Method to initialize and load animations (can be done at runtime)
            public void LoadAnimations()
            {
                //Load animations from folder, if they exist
                AnimationClip[] clips = Resources.LoadAll<AnimationClip>("Animations/");

                foreach (var clip in clips)
                {
                    // Add to main dictionary
                    // animationClips[clip.name] = clip;
                }
            }

            public string NormalizeAnimationName(string animationName, string category = null)
            {
                if (!string.IsNullOrEmpty(category))
                {
                    Log.LogInfo($"Searching for animation {animationName} in {category}");
                    // This could be the resolved name or null if it can't be found
                    var clip = GetAnimation(animationName, category);
                    if (clip != null)
                    {
                        Log.LogInfo($"Found animation for {animationName}: {clip.name} in {category}.");
                    }
                    return clip != null ? clip.name : null;
                }
                else
                {
                    var clip = GetAnimation(animationName);
                    Log.LogInfo($"Found animation for {animationName}: {clip.name} without a category.");
                    return clip == null ? null : clip.name;
                }
            }

            // Get an animation by its name
            public AnimationClip GetAnimation(string name, string category = null)
            {
                // Normalize input for case-insensitive matching
                name = name.ToLowerInvariant();

                // Check if the category exists
                if (!string.IsNullOrEmpty(category) && animationClips.ContainsKey(category))
                {
                    var animationsInCategory = animationClips[category];

                    // Exact match by key
                    if (animationsInCategory.ContainsKey(name))
                    {
                        Log.LogInfo($"Found animation by exact key match: {animationsInCategory[name]} in: {category}");
                        return animationsInCategory[name];
                    }

                    // Exact match by clip name (value)
                    foreach (var clip in animationsInCategory.Values)
                    {
                        if (clip.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                        {
                            Log.LogInfo($"Found animation by exact clip name match: {clip.name} in: {category}");
                            return clip;
                        }
                    }

                    // Fallback: Partial match by key
                    foreach (var key in animationsInCategory.Keys)
                    {
                        if (key.Contains(name))
                        {
                            Log.LogInfo($"Found animation by partial key match: {animationsInCategory[key]} in: {category}");
                            return animationsInCategory[key];
                        }
                    }

                    // Fallback: Partial match by clip name (value)
                    foreach (var clip in animationsInCategory.Values)
                    {
                        if (clip.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            Log.LogInfo($"Found animation by partial clip name match: {clip.name} in: {category}");
                            return clip;
                        }
                    }
                    Log.LogInfo($"No animation named: {name} in category: {category}");
                }

                // Check the "general" category before searching all categories
                if (animationClips.ContainsKey("general") && category == null)
                {
                    var generalAnimations = animationClips["general"];

                    // Exact match by key in "general"
                    if (generalAnimations.ContainsKey(name))
                    {
                        Log.LogInfo($"Found animation by exact key match: {generalAnimations[name]} in: General");
                        return generalAnimations[name];
                    }

                    // Exact match by clip name in "general"
                    foreach (var clip in generalAnimations.Values)
                    {
                        if (clip.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                        {
                            Log.LogInfo($"Found animation by exact clip name match: {clip.name} in: General");
                            return clip;
                        }
                    }

                    // Fallback: Partial match by key in "general"
                    foreach (var key in generalAnimations.Keys)
                    {
                        if (key.Contains(name))
                        {
                            Log.LogInfo($"Found animation by partial key match: {generalAnimations[key]} in: General");
                            return generalAnimations[key];
                        }
                    }

                    // Fallback: Partial match by clip name in "general"
                    foreach (var clip in generalAnimations.Values)
                    {
                        if (clip.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            Log.LogInfo($"Found animation by partial clip name match: {clip.name} in: General");
                            return clip;
                        }
                    }
                    Log.LogInfo($"No animation found with the name: {name} in category: General");
                }

                if (category == null)
                {
                    // Global fallback: Search all categories
                    foreach (var allAnimations in animationClips.Values)
                    {
                        // Exact match by key
                        if (allAnimations.ContainsKey(name))
                        {
                            Log.LogInfo($"Found animation by exact key match: {allAnimations[name]} in: AnimationLibrary");
                            return allAnimations[name];
                        }

                        // Exact match by clip name
                        foreach (var clip in allAnimations.Values)
                        {
                            if (clip.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                            {
                                Log.LogInfo($"Found animation by exact clip name match: {clip.name} in: AnimationLibrary");
                                return clip;
                            }
                        }

                        // Fallback: Partial match by key
                        foreach (var key in allAnimations.Keys)
                        {
                            if (key.Contains(name))
                            {
                                Log.LogInfo($"Found animation by partial key match: {allAnimations[key]} in: AnimationLibrary");
                                return allAnimations[key];
                            }
                        }

                        // Fallback: Partial match by clip name
                        foreach (var clip in allAnimations.Values)
                        {
                            if (clip.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                Log.LogInfo($"Found animation by partial clip name match: {clip.name} in: AnimationLibrary");
                                return clip;
                            }
                        }
                    }
                }
                // Fail state: No match found
                Log.LogWarning($"No animation found with the name: {name}");
                return null;
            }
        }
    }

    /// <summary>
    /// An Emote Manager that is attached to each player
    /// </summary>
    public class PunkEmotesManager : MonoBehaviour
    {
        public Player _player;
        public Animator _animator;
        private PlayableGraph _playableGraph;
        private AnimationClipPlayable _currentClipPlayable;
        private AnimationLayerMixerPlayable _layerMixerPlayable;
        public AnimatorOverrideController newOverrideController;
        public bool _isAnimationPlaying;
        private string _currentAnimation;
        private string _currentCategory;
        public Dictionary<string, string> overrideAliases = new Dictionary<string, string>();
        public Dictionary<string, string> playerOverrides;
        private Dictionary<Transform, Vector3> savedScales;

        // Saves and restores model scale
        private Dictionary<Transform, Vector3> SaveModelScale(Transform root)
        {
            var transformScales = new Dictionary<Transform, Vector3>();
            foreach (Transform child in root.GetComponentsInChildren<Transform>())
            {
                transformScales[child] = child.localScale;
            }
            return transformScales;
        }

        private void RestoreModelScale(Transform root, Dictionary<Transform, Vector3> transformScales)
        {
            foreach (var kvp in transformScales)
            {
                kvp.Key.localScale = kvp.Value;
            }
        }

        // Initializes PlayableGraph and Animator
        private void Awake()
        {
            StartCoroutine(WaitForAnimatorAndConnection());
            this._animator = GetComponent<Animator>();
            this._playableGraph = PlayableGraph.Create();
            this._player = GetComponent<Player>();
            this._currentClipPlayable = AnimationClipPlayable.Create(_playableGraph, null);

        }

        private IEnumerator WaitForAnimatorAndConnection()
        {
            this._player = GetComponent<Player>();
            if (_player == null)
            {
                Log.LogError("PunkEmotesManager must be attached to a Player object.");
                yield break;
            }

            while (this._player._pVisual == null || this._player._pVisual._visualAnimator == null || !NetworkServer.active)
            {
                yield return null; // Wait for the next frame
            }

            this._animator = _player._pVisual._visualAnimator.GetComponent<Animator>();
            playerOverrides = JsonConvert.DeserializeObject<Dictionary<string, string>>(playerOverridesJson.Value);

            if (this._animator != null)
            {
                var originalController = _animator.runtimeAnimatorController as AnimatorOverrideController;
                this.newOverrideController = new AnimatorOverrideController(originalController);
                List<KeyValuePair<AnimationClip, AnimationClip>> defaultOverrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                originalController.GetOverrides(defaultOverrides);

                foreach (var kvp in defaultOverrides)
                {
                    newOverrideController[kvp.Key] = kvp.Value;
                }



                overrideAliases = new Dictionary<string, string>
                {
                    ["idle"] = "player_idle",
                    ["idle2"] = "player_idle02",
                    ["move"] = "player_move",
                    ["walk"] = "player_move",
                    ["run"] = "player_move",
                };

                InitializeGraph(this._animator);
                Log.LogInfo($"Attached PunkEmotesManager to player: {this._player._nickname}");
                // Send sync request handled by PunkEmotesNetwork
                Log.LogInfo($"CmdSyncRequest sent from our current ID of: {_player.connectionToClient}");
                PunkEmotesNetwork.Instance.Cmd_SyncRequest(_player.connectionToClient);
                SendLocalMessage("Welcome to PunkEmotes! Use '/em help' to get command info!");

                // Apply saved overrides from config
                if (playerOverrides != null && playerOverrides.Count > 0)
                {
                    var overridesCopy = new Dictionary<string, string>(playerOverrides); // Copy for safe iteration
                    foreach (var kvp in overridesCopy)
                    {
                        ApplyPunkOverrides(this, kvp.Value, kvp.Key);
                        PunkEmotesNetwork.Instance.Cmd_OverrideChange(this, kvp.Value, kvp.Key);
                    }
                }
            }
            else
            {
                Log.LogError("Animator component not found on PlayerVisual.");
            }
        }

        // Initializes the PlayableGraph for the animation system
        public void InitializeGraph(Animator animator)
        {
            if (animator == null)
            {
                Log.LogError("Animator is null.");
                return;
            }

            this._playableGraph = PlayableGraph.Create("AnimationGraph");
            this._layerMixerPlayable = AnimationLayerMixerPlayable.Create(_playableGraph, 2);  // Two layers: default and custom
            var playableOutput = AnimationPlayableOutput.Create(_playableGraph, "AnimationOutput", animator);
            playableOutput.SetSourcePlayable(_layerMixerPlayable);
        }

        public void GetPlayerAnimationState(PunkEmotesManager remoteEmotesManager)
        {
            if (remoteEmotesManager == null)
            {
                Log.LogError("Received a null PunkEmotesManager from the server! This shouldn't happen.");
                return;
            }

            if (remoteEmotesManager._isAnimationPlaying == true)
            {
                PlayAnimationClip(remoteEmotesManager, remoteEmotesManager._currentAnimation, remoteEmotesManager._currentCategory);
            }
        }

        public void GetPlayerAnimationOverrides(PunkEmotesManager remoteEmotesManager)
        {
            foreach (var kvp in remoteEmotesManager.playerOverrides)
            {
                remoteEmotesManager.ApplyPunkOverrides(remoteEmotesManager, kvp.Key, kvp.Value);
            }
        }

        public void ApplyPunkOverrides(PunkEmotesManager emotesManager, string animationName, string overrideTarget)
        {
            if (PunkEmotesLibrary.Instance == null)
            {
                Log.LogError("PunkEmotesLibrary.Instance is null.");
                return;
            }

            AnimationClip animation = PunkEmotesLibrary.Instance.GetAnimation(animationName, "override" + overrideTarget);

            if (animation == null)
            {
                Log.LogWarning($"No override animation named {animationName} found as partial or whole match!");
                return;
            }

            // Ensure newOverrideController is initialized
            if (emotesManager.newOverrideController == null)
            {
                Log.LogError("newOverrideController is null.");
                return;
            }

            // Create an empty list to iterate over
            List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();

            // Populate the list with the current overrides from the AnimatorOverrideController
            emotesManager.newOverrideController.GetOverrides(overrides);

            // Check if the override target exists and apply the new animation
            bool mapped = false;
            foreach (var kvp in overrides)
            {
                if (!mapped && kvp.Key?.name == overrideTarget)
                {
                    emotesManager.newOverrideController[kvp.Key] = animation;
                    mapped = true;
                    break;
                }
            }

            if (!mapped)
            {
                Log.LogWarning($"{overrideTarget} not found as a target for override.");
                return;
            }

            // Ensure playerOverrides is initialized
            if (playerOverrides == null)
            {
                Log.LogError("playerOverrides is null.");
                playerOverrides = new Dictionary<string, string>();
            }

            // Assign the updated override controller
            emotesManager._animator.runtimeAnimatorController = emotesManager.newOverrideController;

            // Update the player's override list
            if (playerOverrides.ContainsKey(overrideTarget))
            {
                playerOverrides[overrideTarget] = animation.name;
                Log.LogInfo($"Updated override: {overrideTarget} -> {animation.name}");
            }
            else
            {
                playerOverrides.Add(overrideTarget, animation.name);
                Log.LogInfo($"Added new override: {overrideTarget} -> {animation.name}");
            }

            Log.LogInfo($"Applied override: {overrideTarget} -> {animation.name}");
            SaveOverrides(this.playerOverrides, animationName, overrideTarget);
        }

        public void RemoveOverride(PunkEmotesManager emotesManager, string overrideTarget)
        {
            // Get the default race animator based on the player's race
            string raceAnimatorFBX = emotesManager._player._pVisual._playerRaceModel._scriptablePlayerRace._raceName.ToLowerInvariant();
            AnimatorOverrideController defaultRaceAnimator = AnimationConstructor.raceAnimators[raceAnimatorFBX].runtimeAnimatorController as AnimatorOverrideController;

            if (defaultRaceAnimator != null)
            {
                List<KeyValuePair<AnimationClip, AnimationClip>> defaultOverrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                defaultRaceAnimator.GetOverrides(defaultOverrides);

                if (overrideTarget == "all")
                {
                    // Clear all overrides
                    playerOverridesJson.Value = null;
                    playerOverrides.Clear();

                    foreach (var kvp in defaultOverrides)
                    {
                        newOverrideController[kvp.Key] = kvp.Value;
                    }
                }
                else
                {
                    // Handle removing a specific override
                    if (playerOverrides.ContainsKey(overrideTarget))
                    {
                        // Find the default animation clip for this target
                        AnimationClip defaultClip = null;

                        foreach (var kvp in defaultOverrides)
                        {
                            if (kvp.Key.name == overrideTarget)
                            {
                                defaultClip = kvp.Value;
                                break;
                            }
                        }

                        if (defaultClip != null)
                        {
                            // Apply the default clip to the override controller
                            emotesManager.newOverrideController[overrideTarget] = defaultClip;

                            // Remove it from the playerOverrides dictionary
                            playerOverrides.Remove(overrideTarget);

                            // Save changes to overrides
                            SaveOverrides(emotesManager.playerOverrides, null, overrideTarget, true);
                        }
                        else
                        {
                            Log.LogWarning($"No default animation found for override target: {overrideTarget}");
                        }
                    }
                    else
                    {
                        Log.LogWarning($"No override exists for target: {overrideTarget}");
                    }
                }
            }
            else
            {
                Log.LogError("Default race animator is null. Unable to remove overrides.");
            }
        }

        public void ListAvailableOverrides()
        {
            List<KeyValuePair<AnimationClip, AnimationClip>> currentOverrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();

            // Get the overrides
            newOverrideController.GetOverrides(currentOverrides);

            // Check if any overrides exist
            if (currentOverrides.Count == 0)
            {
                Log.LogError("No animations found to override!");
                return;  // Exit early if there are no overrides
            }

            // Log the number of overrides found
            Log.LogInfo($"Total Overrides: {currentOverrides.Count}");

            // Iterate through the list of overrides and log them
            foreach (var kvp in currentOverrides)
            {
                if (kvp.Value != null)
                {
                    SendLocalMessage($"Target: {kvp.Key.name}");
                    SendLocalMessage($"Override: {kvp.Value.name}");
                    Log.LogInfo($"Target: {kvp.Key.name}");
                    Log.LogInfo($"Override: {kvp.Value.name}");
                }
            }
        }

        // Plays animation and applies it to the graph
        public void PlayAnimationClip(PunkEmotesManager emotesManager, string animationName, string animationCategory = null)
        {
            AnimationClip animationClip = PunkEmotesLibrary.Instance.GetAnimation(animationName, animationCategory);

            if (emotesManager._playableGraph.IsValid() == false)
            {
                emotesManager.InitializeGraph(emotesManager._animator);
            }

            if (animationClip == null)
            {
                Log.LogError($"AnimationClip is null.");
                return;
            }

            emotesManager.savedScales = SaveModelScale(emotesManager.transform);
            emotesManager.CrossfadeToCustomAnimation(emotesManager, animationClip);

            var clipPlayable = AnimationClipPlayable.Create(emotesManager._playableGraph, animationClip);
            emotesManager._layerMixerPlayable.ConnectInput(1, clipPlayable, 0);
            emotesManager._layerMixerPlayable.SetInputWeight(1, 1f);  // Full weight for custom animation
            emotesManager._layerMixerPlayable.SetInputWeight(0, 0f);  // No weight on default animation
            emotesManager._playableGraph.Play();
            emotesManager._currentClipPlayable = clipPlayable;
            emotesManager._isAnimationPlaying = true;
            emotesManager._currentAnimation = animationName;
            emotesManager._currentCategory = animationCategory;

            Log.LogInfo($"Playing animation clip: {animationName} for player: {emotesManager._player._nickname}");
        }

        // Stops the animation and restores default state
        public void StopAnimation(PunkEmotesManager emotesManager)
        {
            if (emotesManager._isAnimationPlaying && emotesManager._playableGraph.IsValid())
            {
                emotesManager._layerMixerPlayable.SetInputWeight(1, 0f);  // Fade out custom animation
                emotesManager._layerMixerPlayable.SetInputWeight(0, 1f);  // Fade in default animation
                emotesManager._playableGraph.Stop();
                emotesManager._currentClipPlayable.Destroy();
                emotesManager._currentAnimation = null;
                emotesManager._isAnimationPlaying = false;
                RestoreModelScale(emotesManager._player._pVisual._raceModelAnimManager.transform, emotesManager.savedScales);
                // Send stop animation command handled by PunkEmotesNetwork
                PunkEmotesNetwork.Instance.Cmd_StopAnimation(emotesManager);
                Log.LogInfo($"Stopped custom animation for player: {emotesManager._player._nickname}.");
            }
        }

        // Crossfade to a custom animation
        private void CrossfadeToCustomAnimation(PunkEmotesManager emotesManager, AnimationClip animationClip, float crossfadeDuration = 0.3f)
        {
            if (emotesManager._animator.runtimeAnimatorController is AnimatorOverrideController currentOverride)
            {
                int baseLayerIndex = 0;
                AnimatorStateInfo currentState = emotesManager._animator.GetCurrentAnimatorStateInfo(baseLayerIndex);
                string currentStateName = currentState.IsName("Walk") ? "Walk" :
                                          currentState.IsName("Run") ? "Run" :
                                          currentState.IsName("Idle") ? "Idle" :
                                          currentState.IsName("Jump") ? "Jump" :
                                          currentState.IsName("Dash") ? "Dash" :
                                          null;

                currentOverride[currentStateName] = animationClip;
                emotesManager._animator.CrossFade(currentStateName, crossfadeDuration);
            }
        }
    }

    public static class PlayerRegistry
    {
        public static Dictionary<uint, PlayerEntry> _playersByNetId = new Dictionary<uint, PlayerEntry>();

        public class PlayerEntry
        {
            public string Nickname { get; set; }
            public Player PlayerInstance { get; set; }
            public PunkEmotesManager EmotesManager { get; set; }
        }

        // Register a player and their PunkEmotesManager, called by the host
        public static void RegisterPlayer(uint netId, string nickname, Player player, PunkEmotesManager emotesManager)
        {
            if (player != null && emotesManager != null && !_playersByNetId.ContainsKey(netId))
            {
                _playersByNetId[netId] = new PlayerEntry
                {
                    Nickname = nickname,
                    PlayerInstance = player,
                    EmotesManager = emotesManager
                };
            }
        }

        // Unregister a player and their PunkEmotesManager, called by the host when player disconnects
        public static void UnregisterPlayer(uint netId)
        {
            _playersByNetId.Remove(netId);
        }

        // Clear all players from the registry (useful for cleanup on level reset or server shutdown)
        public static void ClearRegistry()
        {
            _playersByNetId.Clear();
        }

        // Get a player's PunkEmotesManager by their netId
        public static PunkEmotesManager GetEmotesManagerByNetId(uint netId)
        {
            return _playersByNetId.TryGetValue(netId, out var entry) ? entry.EmotesManager : null;
        }

        // Get a player instance by their netId
        public static Player GetPlayerByNetId(uint netId)
        {
            return _playersByNetId.TryGetValue(netId, out var entry) ? entry.PlayerInstance : null;
        }

        // Get a player instance by their nickname
        public static Player GetPlayerByNickname(string nickname)
        {
            foreach (var entry in _playersByNetId.Values)
            {
                if (entry.Nickname == nickname)
                {
                    return entry.PlayerInstance;
                }
            }
            return null;
        }
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

            if (PunkEmotesLibrary.Instance == null)
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
                                string clipSitInit = PunkEmotesLibrary.Instance.GetAnimation(parts[2], "override_playerEmote_sitInit").name;
                                string clipSitLoop = PunkEmotesLibrary.Instance.GetAnimation(parts[2], "override_playerEmote_sitLoop").name;

                                emotesManager.ApplyPunkOverrides(emotesManager, clipSitInit, "_playerEmote_sitInit");
                                emotesManager.ApplyPunkOverrides(emotesManager, clipSitLoop, "_playerEmote_sitLoop");

                                PunkEmotesNetwork.Instance.Cmd_OverrideChange(emotesManager, clipSitInit, "_playerEmote_sitInit");
                                PunkEmotesNetwork.Instance.Cmd_OverrideChange(emotesManager, clipSitLoop, "_playerEmote_sitLoop");
                            }
                            else if (string.Equals(parts[2], "sit2", StringComparison.OrdinalIgnoreCase))
                            {
                                string clipSitInit = PunkEmotesLibrary.Instance.GetAnimation(parts[2], "override_playerEmote_sitInit02").name;
                                string clipSitLoop = PunkEmotesLibrary.Instance.GetAnimation(parts[2], "override_playerEmote_sitLoop02").name;

                                emotesManager.ApplyPunkOverrides(emotesManager, clipSitInit, "_playerEmote_sitInit02");
                                emotesManager.ApplyPunkOverrides(emotesManager, clipSitLoop, "_playerEmote_sitLoop02");

                                PunkEmotesNetwork.Instance.Cmd_OverrideChange(emotesManager, clipSitInit, "_playerEmote_sitInit02");
                                PunkEmotesNetwork.Instance.Cmd_OverrideChange(emotesManager, clipSitLoop, "_playerEmote_sitLoop02");
                            }
                            else if (PunkEmotesLibrary.Instance.NormalizeAnimationName(parts[2], "override" + targetName) != null)
                            {
                                {
                                    // Handle general override cases using the resolved targetName
                                    string clip = PunkEmotesLibrary.Instance.GetAnimation(parts[2], "override" + targetName).name;

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
                    if (parts.Length == 2 && PunkEmotesLibrary.Instance.NormalizeAnimationName(parts[1], parts[0]) != null)
                    {
                        string clip = PunkEmotesLibrary.Instance.GetAnimation(parts[1], parts[0]).name;
                        emotesManager.PlayAnimationClip(emotesManager, clip, parts[0]);
                        PunkEmotesNetwork.Instance.Cmd_AnimationChange(emotesManager, clip, parts[0]);
                    }
                    else if (parts.Length == 1 && PunkEmotesLibrary.Instance.NormalizeAnimationName(parts[0]) != null)
                    {
                        string clip = PunkEmotesLibrary.Instance.GetAnimation(parts[0]).name;
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
                raceAnimatorReset = true;
                PlayerRegistry.ClearRegistry();
                //LogInfo("Reset call for animators");
            }
        }

        [HarmonyPatch(typeof(PlayerVisual), "Iterate_AnimationCallback")]
        public class LoadFBX
        {
            static void Postfix(PlayerVisual __instance, ref string _animName, ref float _animLayer)
            {
                if (raceAnimatorReset == true)
                {
                    LoadRaceFBXs();
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
