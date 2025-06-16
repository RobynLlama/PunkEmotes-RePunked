using System.Collections;
using System.Collections.Generic;
using Mirror;
using Newtonsoft.Json;
using PunkEmotes.Internals;
using PunkEmotes.Utils;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PunkEmotes.Components;

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
      PunkEmotesPlugin.Log.LogError("PunkEmotesManager must be attached to a Player object.");
      yield break;
    }

    while (this._player._pVisual == null || this._player._pVisual._visualAnimator == null || !NetworkServer.active)
    {
      yield return null; // Wait for the next frame
    }

    this._animator = _player._pVisual._visualAnimator.GetComponent<Animator>();
    playerOverrides = JsonConvert.DeserializeObject<Dictionary<string, string>>(PunkEmotesPlugin.playerOverridesJson.Value);

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
      PunkEmotesPlugin.Log.LogInfo($"Attached PunkEmotesManager to player: {this._player._nickname}");
      // Send sync request handled by PunkEmotesNetwork
      PunkEmotesPlugin.Log.LogInfo($"CmdSyncRequest sent from our current ID of: {_player.connectionToClient}");
      PunkEmotesNetwork.Instance.Cmd_SyncRequest(_player.connectionToClient);
      Utilities.SendLocalMessage("Welcome to PunkEmotes! Use '/em help' to get command info!");

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
      PunkEmotesPlugin.Log.LogError("Animator component not found on PlayerVisual.");
    }
  }

  // Initializes the PlayableGraph for the animation system
  public void InitializeGraph(Animator animator)
  {
    if (animator == null)
    {
      PunkEmotesPlugin.Log.LogError("Animator is null.");
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
      PunkEmotesPlugin.Log.LogError("Received a null PunkEmotesManager from the server! This shouldn't happen.");
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
    if (AnimationConstructor.PunkEmotesLibrary.Instance == null)
    {
      PunkEmotesPlugin.Log.LogError("PunkEmotesLibrary.Instance is null.");
      return;
    }

    AnimationClip animation = AnimationConstructor.PunkEmotesLibrary.Instance.GetAnimation(animationName, "override" + overrideTarget);

    if (animation == null)
    {
      PunkEmotesPlugin.Log.LogWarning($"No override animation named {animationName} found as partial or whole match!");
      return;
    }

    // Ensure newOverrideController is initialized
    if (emotesManager.newOverrideController == null)
    {
      PunkEmotesPlugin.Log.LogError("newOverrideController is null.");
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
      PunkEmotesPlugin.Log.LogWarning($"{overrideTarget} not found as a target for override.");
      return;
    }

    // Ensure playerOverrides is initialized
    if (playerOverrides == null)
    {
      PunkEmotesPlugin.Log.LogError("playerOverrides is null.");
      playerOverrides = new Dictionary<string, string>();
    }

    // Assign the updated override controller
    emotesManager._animator.runtimeAnimatorController = emotesManager.newOverrideController;

    // Update the player's override list
    if (playerOverrides.ContainsKey(overrideTarget))
    {
      playerOverrides[overrideTarget] = animation.name;
      PunkEmotesPlugin.Log.LogInfo($"Updated override: {overrideTarget} -> {animation.name}");
    }
    else
    {
      playerOverrides.Add(overrideTarget, animation.name);
      PunkEmotesPlugin.Log.LogInfo($"Added new override: {overrideTarget} -> {animation.name}");
    }

    PunkEmotesPlugin.Log.LogInfo($"Applied override: {overrideTarget} -> {animation.name}");
    PunkEmotesPlugin.SaveOverrides(this.playerOverrides, animationName, overrideTarget);
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
        PunkEmotesPlugin.playerOverridesJson.Value = null;
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
            PunkEmotesPlugin.SaveOverrides(emotesManager.playerOverrides, null, overrideTarget, true);
          }
          else
          {
            PunkEmotesPlugin.Log.LogWarning($"No default animation found for override target: {overrideTarget}");
          }
        }
        else
        {
          PunkEmotesPlugin.Log.LogWarning($"No override exists for target: {overrideTarget}");
        }
      }
    }
    else
    {
      PunkEmotesPlugin.Log.LogError("Default race animator is null. Unable to remove overrides.");
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
      PunkEmotesPlugin.Log.LogError("No animations found to override!");
      return;  // Exit early if there are no overrides
    }

    // Log the number of overrides found
    PunkEmotesPlugin.Log.LogInfo($"Total Overrides: {currentOverrides.Count}");

    // Iterate through the list of overrides and log them
    foreach (var kvp in currentOverrides)
    {
      if (kvp.Value != null)
      {
        Utilities.SendLocalMessage($"Target: {kvp.Key.name}");
        Utilities.SendLocalMessage($"Override: {kvp.Value.name}");
        PunkEmotesPlugin.Log.LogInfo($"Target: {kvp.Key.name}");
        PunkEmotesPlugin.Log.LogInfo($"Override: {kvp.Value.name}");
      }
    }
  }

  // Plays animation and applies it to the graph
  public void PlayAnimationClip(PunkEmotesManager emotesManager, string animationName, string animationCategory = null)
  {
    AnimationClip animationClip = AnimationConstructor.PunkEmotesLibrary.Instance.GetAnimation(animationName, animationCategory);

    if (emotesManager._playableGraph.IsValid() == false)
    {
      emotesManager.InitializeGraph(emotesManager._animator);
    }

    if (animationClip == null)
    {
      PunkEmotesPlugin.Log.LogError($"AnimationClip is null.");
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

    PunkEmotesPlugin.Log.LogInfo($"Playing animation clip: {animationName} for player: {emotesManager._player._nickname}");
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
      PunkEmotesPlugin.Log.LogInfo($"Stopped custom animation for player: {emotesManager._player._nickname}.");
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
