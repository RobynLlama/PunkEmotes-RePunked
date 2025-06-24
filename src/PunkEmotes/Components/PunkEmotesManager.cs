using System;
using System.Collections;
using System.Collections.Generic;
using CodeTalker.Networking;
using CodeTalker.Packets;
using PunkEmotes.Internals;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PunkEmotes.Components;

public class PunkEmotesManager : MonoBehaviour
{
  private PlayableGraph _playableGraph;

  //Null forgiven for being in Awake
  public Animator _animator = null!;

  private AnimationClipPlayable _currentClipPlayable;

  private AnimationLayerMixerPlayable _layerMixerPlayable;

  //Null forgiven for showing up in the wait for animator CR
  private AnimatorOverrideController newOverrideController = null!;

  public Dictionary<string, List<string>> overrideAliases = [];

  internal static readonly ChatBehaviour.ChatChannel PUNK_NETWORK_CHANNEL = ChatBehaviour.ChatChannel.CHANNEL_TWO;

  private string? _currentAnimation = string.Empty;

  private string? _currentCategory = string.Empty;

  private List<string> playerOverrides = [];

  //Null forgiven for being in Awake
  private Player _player = null!;

  public bool _isAnimationPlaying;

  private void Awake()
  {
    StartCoroutine(WaitForAnimator());
    //These 4 items are null forgiven in the class since they are set here
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
      PunkEmotesPlugin.Log.LogError("PunkEmotesManager must be attached to a Player object.");
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
      PunkEmotesPlugin.Log.LogInfo("Attached PunkEmotesManager to player: " + _player._nickname);
      SendSyncRequest();
    }
    else
    {
      PunkEmotesPlugin.Log.LogError("Animator component not found on PlayerVisual.");
    }
  }

  public void InitializeGraph(Animator animator)
  {
    if (animator == null)
    {
      PunkEmotesPlugin.Log.LogError("Animator is null.");
      return;
    }
    _playableGraph = PlayableGraph.Create("AnimationGraph");
    _layerMixerPlayable = AnimationLayerMixerPlayable.Create(_playableGraph, 2);
    AnimationPlayableOutput val = AnimationPlayableOutput.Create(_playableGraph, "AnimationOutput", animator);
    PlayableOutputExtensions.SetSourcePlayable<AnimationPlayableOutput, AnimationLayerMixerPlayable>(val, _layerMixerPlayable);
  }

  private void SendSyncRequest()
  {
    //string text = $"{PUNK_NETWORK_SIGNATURE_DIRTY}{_player.netId}#ALL#SYNCREQUEST#";
    //GetComponent<ChatBehaviour>().Cmd_SendChatMessage(text, PUNK_NETWORK_CHANNEL);

    PunkAnimationPacket payload = new(_player.netId, "ALL", "SYNCREQUEST", string.Empty, string.Empty);
    CodeTalkerNetwork.SendNetworkPacket(payload);
  }

  private void SendSyncResponse(string target)
  {
    if (_isAnimationPlaying)
    {
      PunkEmotesPlugin.Log.LogInfo("We're sending the animation to " + target);
      SendAnimationCommand(target, "START", _currentAnimation, this, _currentCategory);
    }
    else
    {
      PunkEmotesPlugin.Log.LogInfo("No animation playing, is this correct?");
    }
    if (playerOverrides != null)
    {
      PunkEmotesPlugin.Log.LogInfo("We're sending override info to " + target);
      {
        foreach (var playerOverride in playerOverrides)
        {
          if (playerOverride is not string validOverride)
            continue;

          PunkEmotesPlugin.Log.LogInfo(validOverride);
          string[] array = validOverride.Split('_');
          string animationName = array[0];
          string originOverride = array[1];
          ApplyPunkOverrides(target, this, animationName, originOverride);
        }
        return;
      }
    }
    PunkEmotesPlugin.Log.LogInfo("No override info to send, is this correct?");
  }

  public void ApplyPunkOverrides(string? target, PunkEmotesManager emotesManager, string animationName, string originOverride)
  {
    if (AnimationConstructor.AnimationLibrary.Instance.GetAnimation(animationName, "override") is not AnimationClip animation)
      return;

    List<KeyValuePair<AnimationClip, AnimationClip>> list = [];
    RuntimeAnimatorController runtimeAnimatorController = _animator.runtimeAnimatorController;

    if (runtimeAnimatorController is AnimatorOverrideController val)
    {
      val.GetOverrides(list);
    }

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
      PunkEmotesPlugin.Log.LogError(originOverride + " not found in override mappings.");
      return;
    }
    _animator.runtimeAnimatorController = newOverrideController;
    PunkEmotesPlugin.Log.LogInfo("Applied override: '" + originOverride + "' -> '" + animationName + "'.");
    string item = animationName + "_" + originOverride;
    if (!playerOverrides.Contains(item))
    {
      playerOverrides.Add(item);
    }
    SendAnimationCommand("ALL", "Override", animationName, emotesManager, originOverride);
  }

  public void PlayAnimationClip(string? target, PunkEmotesManager emotesManager, string animationName, string? animationCategory = null)
  {
    if (AnimationConstructor.AnimationLibrary.Instance.GetAnimation(animationName, animationCategory) is not AnimationClip animation)
    {
      PunkEmotesPlugin.Log.LogError("AnimationClip is null.");
      return;
    }
    if (!emotesManager._playableGraph.IsValid())
    {
      emotesManager.InitializeGraph(emotesManager._animator);
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
    PunkEmotesPlugin.Log.LogInfo($"Playing animation clip: {animationName} for player with netId {emotesManager._player.netId}");
  }

  public void StopAnimation(PunkEmotesManager emotesManager)
  {
    if (emotesManager._isAnimationPlaying && emotesManager._playableGraph.IsValid())
    {
      string? animationName = null;
      PlayableExtensions.SetInputWeight<AnimationLayerMixerPlayable>(_layerMixerPlayable, 1, 0f);
      PlayableExtensions.SetInputWeight<AnimationLayerMixerPlayable>(_layerMixerPlayable, 0, 1f);
      emotesManager._playableGraph.Stop();
      PlayableExtensions.Destroy<AnimationClipPlayable>(_currentClipPlayable);
      emotesManager._currentAnimation = null;
      emotesManager._isAnimationPlaying = false;
      SendAnimationCommand("ALL", "STOP", animationName, emotesManager);
      PunkEmotesPlugin.Log.LogInfo($"Stopped custom animation for player with netId {emotesManager._player.netId}.");
    }
  }

  private void CrossfadeToCustomAnimation(PunkEmotesManager emotesManager, AnimationClip animationClip, float crossfadeDuration = 0.3f)
  {
    RuntimeAnimatorController runtimeAnimatorController = emotesManager._animator.runtimeAnimatorController;
    if (runtimeAnimatorController is AnimatorOverrideController val)
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
        PunkEmotesPlugin.Log.LogError("Base layer not found in animator.");
      }
    }
    PunkEmotesPlugin.Log.LogInfo($"Crossfaded to custom animation: {(animationClip).name} with a {crossfadeDuration}s transition.");
  }

  public void CrossfadeToDefaultState()
  {
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
    PunkEmotesPlugin.Log.LogInfo("Crossfaded back to default state (Idle/Walk/Run) after custom animation.");
  }

  public void Dispose(PunkEmotesManager emotesManager)
  {
    if (emotesManager._playableGraph.IsValid())
    {
      emotesManager._playableGraph.Destroy();
      PunkEmotesPlugin.Log.LogInfo("PlayableGraph destroyed.");
    }
  }

  public void SendAnimationCommand(string target, string command, string? animationName, PunkEmotesManager emotesManager, string? categoryOrOrigin = null)
  {
    //string text = $"{PUNK_NETWORK_SIGNATURE_DIRTY}{emotesManager._player.netId}#{target}#{command}#{animationName}#{categoryOrOrigin}";
    //emotesManager.gameObject.GetComponent<ChatBehaviour>().Cmd_SendChatMessage(text, PUNK_NETWORK_CHANNEL);

    PunkAnimationPacket payload = new(emotesManager._player.netId, target, command, animationName ?? string.Empty, categoryOrOrigin ?? string.Empty);
    CodeTalkerNetwork.SendNetworkPacket(payload);
  }

  internal static void HandleChatAnimationMessage(PacketHeader header, PacketBase incPacket)
  {

    if (incPacket is not PunkAnimationPacket packet)
      return;

    if (PlayerRegistry.GetPlayerByNetId(packet.SenderNetworkID) is not Player messageSender)
    {
      PunkEmotesPlugin.Log.LogWarning($"Unable to find player for NetID: {packet.SenderNetworkID}");
      return;
    }

    if (Player._mainPlayer.netId == messageSender.netId)
    {
      PunkEmotesPlugin.Log.LogMessage("Skipping local player's message.");
      return;
    }

    PunkEmotesManager senderManager = messageSender.GetComponent<PunkEmotesManager>();

    //The original doesn't log here
    if (senderManager == null)
      return;

    switch (packet.RequestType.ToLower())
    {
      case "syncrequest":
        senderManager.SendSyncResponse(packet.SenderNetworkID.ToString());
        break;
      case "override":
        if (!string.IsNullOrEmpty(packet.AnimationCategory))
        {
          if (senderManager.overrideAliases.ContainsKey(packet.AnimationCategory))
          {
            List<string> overrides = senderManager.overrideAliases[packet.AnimationCategory];
            string animationName = packet.AnimationName + overrides[2];
            string animationName2 = packet.AnimationName + overrides[3];
            senderManager.ApplyPunkOverrides(null, senderManager, animationName, overrides[0]);
            senderManager.ApplyPunkOverrides(null, senderManager, animationName2, overrides[1]);
          }
          else
          {
            senderManager.ApplyPunkOverrides(null, senderManager, packet.AnimationName, packet.AnimationCategory);
          }
        }
        else
        {
          PunkEmotesPlugin.Log.LogWarning("Override command missing originOverride for animation '" + packet.AnimationName + "'.");
        }
        break;
      case "start":
        if (!string.IsNullOrEmpty(packet.AnimationCategory))
          senderManager.PlayAnimationClip(null, senderManager, packet.AnimationName, packet.AnimationCategory);
        else
          senderManager.PlayAnimationClip(null, senderManager, packet.AnimationName);
        break;
      case "stop":
        senderManager.StopAnimation(senderManager);
        break;
      default:
        PunkEmotesPlugin.Log.LogWarning($"Unknown request '{packet.RequestType}' received in PUNKEMOTES message.");
        return;
    }
  }
}
