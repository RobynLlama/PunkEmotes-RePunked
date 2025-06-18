using System;
using System.Collections;
using System.Collections.Generic;
using PunkEmotes.Internals;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PunkEmotes.Components;

public class PunkEmotesManager : MonoBehaviour
{
  private PlayableGraph _playableGraph;

  public Animator _animator;

  private AnimationClipPlayable _currentClipPlayable;

  private AnimationLayerMixerPlayable _layerMixerPlayable;

  private AnimatorOverrideController newOverrideController;

  public Dictionary<string, List<string>> overrideAliases = new Dictionary<string, List<string>>();

  private string _currentAnimation;

  private string _currentCategory;

  private List<string> playerOverrides = new List<string>();

  private Player _player;

  public bool _isAnimationPlaying;

  private void Awake()
  {
    StartCoroutine(WaitForAnimator());
    _animator = GetComponent<Animator>();
    _playableGraph = PlayableGraph.Create();
    _player = GetComponent<Player>();
    _currentClipPlayable = AnimationClipPlayable.Create(_playableGraph, null);
    overrideAliases["sit"] = ["_playeremote_sitinit", "_playeremote_sitloop", "_sitInit", "_sitLoop"];
    overrideAliases["sit2"] = ["_playeremote_sitinit02", "_playeremote_sitloop02", "_sitInit02", "_sitLoop02"];
  }

  private IEnumerator WaitForAnimator()
  {
    _player = GetComponent<Player>();
    if (_player == null)
    {
      PunkEmotesPlugin.LogError("PunkEmotesManager must be attached to a Player object.");
      yield break;
    }
    while (_player._pVisual == null || _player._pVisual._visualAnimator == null)
    {
      yield return null;
    }
    _animator = _player._pVisual._visualAnimator.GetComponent<Animator>();
    if (_animator != null)
    {
      newOverrideController = new AnimatorOverrideController(_animator.runtimeAnimatorController);
      InitializeGraph(_animator);
      PunkEmotesPlugin.LogInfo("Attached PunkEmotesManager to player: " + _player._nickname);
      SendSyncRequest();
    }
    else
    {
      PunkEmotesPlugin.LogError("Animator component not found on PlayerVisual.");
    }
  }

  public void InitializeGraph(Animator animator)
  {
    if (animator == null)
    {
      PunkEmotesPlugin.LogError("Animator is null.");
      return;
    }
    _playableGraph = PlayableGraph.Create("AnimationGraph");
    _layerMixerPlayable = AnimationLayerMixerPlayable.Create(_playableGraph, 2);
    AnimationPlayableOutput val = AnimationPlayableOutput.Create(_playableGraph, "AnimationOutput", animator);
    PlayableOutputExtensions.SetSourcePlayable<AnimationPlayableOutput, AnimationLayerMixerPlayable>(val, _layerMixerPlayable);
  }

  private void SendSyncRequest()
  {
    string text = $"<>#PUNKEMOTES#{_player.netId}#ALL#SYNCREQUEST#";
    GetComponent<ChatBehaviour>().Cmd_SendChatMessage(text, (ChatBehaviour.ChatChannel)3);
  }

  private void SendSyncResponse(string target)
  {
    if (_isAnimationPlaying)
    {
      PunkEmotesPlugin.LogInfo("We're sending the animation to " + target);
      SendAnimationCommand(target, "START", _currentAnimation, this, _currentCategory);
    }
    else
    {
      PunkEmotesPlugin.LogInfo("No animation playing, is this correct?");
    }
    if (playerOverrides != null)
    {
      PunkEmotesPlugin.LogInfo("We're sending override info to " + target);
      {
        foreach (string playerOverride in playerOverrides)
        {
          PunkEmotesPlugin.LogInfo(playerOverride ?? "");
          string[] array = playerOverride.Split('_');
          string animationName = array[0];
          string originOverride = array[1];
          ApplyPunkOverrides(target, this, animationName, originOverride);
        }
        return;
      }
    }
    PunkEmotesPlugin.LogInfo("No override info to send, is this correct?");
  }

  public void ApplyPunkOverrides(string target, PunkEmotesManager emotesManager, string animationName, string originOverride)
  {
    AnimationClip animation = AnimationConstructor.AnimationLibrary.Instance.GetAnimation(animationName, "override");
    if (animation == null)
    {
      return;
    }
    RuntimeAnimatorController runtimeAnimatorController = _animator.runtimeAnimatorController;
    AnimatorOverrideController val = (AnimatorOverrideController)((runtimeAnimatorController is AnimatorOverrideController) ? runtimeAnimatorController : null);
    List<KeyValuePair<AnimationClip, AnimationClip>> list = new List<KeyValuePair<AnimationClip, AnimationClip>>();
    val.GetOverrides(list);
    foreach (KeyValuePair<AnimationClip, AnimationClip> item2 in list)
    {
      newOverrideController[item2.Key] = item2.Value;
    }
    bool flag = false;
    foreach (KeyValuePair<AnimationClip, AnimationClip> item3 in list)
    {
      AnimationClip key = item3.Key;
      if (((key != null) ? (key).name.ToLowerInvariant() : null) == originOverride)
      {
        newOverrideController[item3.Key] = animation;
        flag = true;
        break;
      }
    }
    if (!flag)
    {
      PunkEmotesPlugin.LogError(originOverride + " not found in override mappings.");
      return;
    }
    _animator.runtimeAnimatorController = newOverrideController;
    PunkEmotesPlugin.LogInfo("Applied override: '" + originOverride + "' -> '" + animationName + "'.");
    string item = animationName + "_" + originOverride;
    if (!playerOverrides.Contains(item))
    {
      playerOverrides.Add(item);
    }
    SendAnimationCommand("ALL", "Override", animationName, emotesManager, originOverride);
  }

  public void PlayAnimationClip(string target, PunkEmotesManager emotesManager, string animationName, string animationCategory = null)
  {
    AnimationClip animation = AnimationConstructor.AnimationLibrary.Instance.GetAnimation(animationName, animationCategory);
    if (!emotesManager._playableGraph.IsValid())
    {
      emotesManager.InitializeGraph(emotesManager._animator);
    }
    if (animation == null)
    {
      PunkEmotesPlugin.LogError("AnimationClip is null.");
      return;
    }
    emotesManager.CrossfadeToCustomAnimation(emotesManager, animation);
    AnimationClipPlayable val = AnimationClipPlayable.Create(emotesManager._playableGraph, animation);
    PlayableExtensions.ConnectInput<AnimationLayerMixerPlayable, AnimationClipPlayable>(_layerMixerPlayable, 1, val, 0);
    PlayableExtensions.SetInputWeight<AnimationLayerMixerPlayable>(_layerMixerPlayable, 1, 1f);
    PlayableExtensions.SetInputWeight<AnimationLayerMixerPlayable>(_layerMixerPlayable, 0, 0f);
    emotesManager._playableGraph.Play();
    emotesManager._currentClipPlayable = val;
    emotesManager._isAnimationPlaying = true;
    emotesManager._currentAnimation = animationName;
    emotesManager._currentCategory = animationCategory;
    if (Player._mainPlayer.netId == _player.netId)
    {
      SendAnimationCommand("ALL", "START", animationName, emotesManager, animationCategory);
    }
    PunkEmotesPlugin.LogInfo($"Playing animation clip: {animationName} for player with netId {emotesManager._player.netId}");
  }

  public void StopAnimation(PunkEmotesManager emotesManager)
  {
    //IL_0022: Unknown result type (might be due to invalid IL or missing references)
    //IL_0034: Unknown result type (might be due to invalid IL or missing references)
    //IL_0052: Unknown result type (might be due to invalid IL or missing references)
    if (emotesManager._isAnimationPlaying && emotesManager._playableGraph.IsValid())
    {
      string animationName = null;
      PlayableExtensions.SetInputWeight<AnimationLayerMixerPlayable>(_layerMixerPlayable, 1, 0f);
      PlayableExtensions.SetInputWeight<AnimationLayerMixerPlayable>(_layerMixerPlayable, 0, 1f);
      emotesManager._playableGraph.Stop();
      PlayableExtensions.Destroy<AnimationClipPlayable>(_currentClipPlayable);
      emotesManager._currentAnimation = null;
      emotesManager._isAnimationPlaying = false;
      SendAnimationCommand("ALL", "STOP", animationName, emotesManager);
      PunkEmotesPlugin.LogInfo($"Stopped custom animation for player with netId {emotesManager._player.netId}.");
    }
  }

  private void CrossfadeToCustomAnimation(PunkEmotesManager emotesManager, AnimationClip animationClip, float crossfadeDuration = 0.3f)
  {
    //IL_0037: Unknown result type (might be due to invalid IL or missing references)
    //IL_003c: Unknown result type (might be due to invalid IL or missing references)
    RuntimeAnimatorController runtimeAnimatorController = emotesManager._animator.runtimeAnimatorController;
    AnimatorOverrideController val = (AnimatorOverrideController)((runtimeAnimatorController is AnimatorOverrideController) ? runtimeAnimatorController : null);
    if (val != null)
    {
      int num = 0;
      if (emotesManager._animator.layerCount > num)
      {
        AnimatorStateInfo currentAnimatorStateInfo = emotesManager._animator.GetCurrentAnimatorStateInfo(num);
        string text = currentAnimatorStateInfo.IsName("Idle") ? "Idle" : "Default";
        val[text] = animationClip;
        emotesManager._animator.CrossFade(text, crossfadeDuration, num);
      }
      else
      {
        PunkEmotesPlugin.LogError("Base layer not found in animator.");
      }
    }
    PunkEmotesPlugin.LogInfo($"Crossfaded to custom animation: {(animationClip).name} with a {crossfadeDuration}s transition.");
  }

  public void CrossfadeToDefaultState()
  {
    //IL_0008: Unknown result type (might be due to invalid IL or missing references)
    //IL_000d: Unknown result type (might be due to invalid IL or missing references)
    AnimatorStateInfo currentAnimatorStateInfo = _animator.GetCurrentAnimatorStateInfo(0);
    if (currentAnimatorStateInfo.IsName("Walk") ||
        currentAnimatorStateInfo.IsName("Run") ||
        currentAnimatorStateInfo.IsName("Jump") ||
        currentAnimatorStateInfo.IsName("Dash"))
    {
      _animator.CrossFade(currentAnimatorStateInfo.fullPathHash, 0.2f);
    }
    else
    {
      _animator.CrossFade("Idle", 0.2f);
    }
    PunkEmotesPlugin.LogInfo("Crossfaded back to default state (Idle/Walk/Run) after custom animation.");
  }

  public void Dispose(PunkEmotesManager emotesManager)
  {
    if (emotesManager._playableGraph.IsValid())
    {
      emotesManager._playableGraph.Destroy();
      PunkEmotesPlugin.LogInfo("PlayableGraph destroyed.");
    }
  }

  public void SendAnimationCommand(string target, string command, string animationName, PunkEmotesManager emotesManager, string categoryOrOrigin = null)
  {
    string text = $"<>#PUNKEMOTES#{emotesManager._player.netId}#{target}#{command}#{animationName}#{categoryOrOrigin}";
    _player.GetComponent<ChatBehaviour>().Cmd_SendChatMessage(text, (ChatBehaviour.ChatChannel)3);
  }

  public void HandleChatAnimationMessage(string message)
  {
    if (string.IsNullOrEmpty(message))
    {
      return;
    }
    string[] array = message.ToLower().Split('#', StringSplitOptions.None);
    if (array.Length >= 5)
    {
      if (uint.TryParse(array[2], out var result))
      {
        string target = array[3];
        string request = array[4];
        string aniName = array[5];
        string? aniCat = array.Length > 6 ? array[6] : null;
        if (result == Player._mainPlayer.netId)
        {
          PunkEmotesPlugin.LogInfo("Skipping local player animation reprocessing to prevent infinite loop.");
        }
        else
        {
          if (!(target == "all") && (!uint.TryParse(target, out var result2) || result2 != Player._mainPlayer.netId))
          {
            return;
          }
          Player playerByNetId = PunkEmotesPlugin.PlayerRegistry.GetPlayerByNetId(result);
          if (playerByNetId != null)
          {
            PunkEmotesManager component = playerByNetId.GetComponent<PunkEmotesManager>();
            if (component != null)
            {
              switch (request)
              {
                case "syncrequest":
                  PunkEmotesPlugin.LogInfo("Sync requested, sending response:");
                  SendSyncResponse(result.ToString());
                  break;
                case "start":
                  if (!string.IsNullOrEmpty(aniCat))
                  {
                    component.PlayAnimationClip(null, component, aniName, aniCat);
                  }
                  else
                  {
                    component.PlayAnimationClip(null, component, aniName);
                  }
                  break;
                case "override":
                  if (!string.IsNullOrEmpty(aniCat))
                  {
                    if (overrideAliases.ContainsKey(aniCat))
                    {
                      List<string> list = overrideAliases[aniCat];
                      string animationName = aniName + list[2];
                      string animationName2 = aniName + list[3];
                      component.ApplyPunkOverrides(null, component, animationName, list[0]);
                      component.ApplyPunkOverrides(null, component, animationName2, list[1]);
                    }
                    else
                    {
                      component.ApplyPunkOverrides(null, component, aniName, aniCat);
                    }
                  }
                  else
                  {
                    PunkEmotesPlugin.LogWarning("Override command missing originOverride for animation '" + aniName + "'.");
                  }
                  break;
                case "stop":
                  component.StopAnimation(component);
                  break;
                default:
                  PunkEmotesPlugin.LogWarning("Unknown command '" + request + "' received in PUNKEMOTES message.");
                  break;
              }
            }
            else
            {
              PunkEmotesPlugin.LogWarning($"PunkEmotesManager not found for sender player with netId '{result}'.");
            }
          }
          else
          {
            PunkEmotesPlugin.LogWarning($"Sender player with netId '{result}' not found.");
          }
        }
      }
      else
      {
        PunkEmotesPlugin.LogWarning("Failed to parse sender's netId from message: " + array[2]);
      }
    }
    else
    {
      PunkEmotesPlugin.LogWarning("Invalid PUNKEMOTES message format. Insufficient parts.");
    }
  }
}
