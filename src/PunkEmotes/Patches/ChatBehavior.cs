using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using PunkEmotes.Components;
using PunkEmotes.Internals;
using UnityEngine;

namespace PunkEmotes.Patches;

internal static class ChatBehaviour_Patches
{
  private static MethodInfo rpcMethod = typeof(ChatBehaviour).GetMethod("Rpc_RecieveChatMessage", BindingFlags.Instance | BindingFlags.NonPublic);

  [HarmonyPatch(typeof(ChatBehaviour), nameof(ChatBehaviour.Send_ChatMessage))]
  [HarmonyPrefix]
  private static bool Send_ChatMessage_Prefix(ref string _message, ChatBehaviour __instance)
  {
    if (string.IsNullOrEmpty(_message))
    {
      return true;
    }
    if (!_message.StartsWith("/em ", StringComparison.OrdinalIgnoreCase))
    {
      return true;
    }
    PunkEmotesManager emotesManagerByNetId = PlayerRegistry.GetEmotesManagerByNetId(Player._mainPlayer.netId);
    string text = _message.Substring(4).Trim();
    string[] array = text.Split(' ');
    string text2 = array[0].ToLower();
    string text3 = text2;
    if (!(text3 == "overrides"))
    {
      if (text3 == "help")
      {
        PunkEmotesPlugin.SendChatMessage("Commands: '/em animation_name (or race)'");
        PunkEmotesPlugin.SendChatMessage("Commands: '/em category animation_name (or race)'");
        PunkEmotesPlugin.SendChatMessage("Categories: 'sit', 'dance'");
        PunkEmotesPlugin.SendChatMessage("Test animation: '/em 02'");
        PunkEmotesPlugin.Log.LogInfo("Available commands: overrides, help");
        return false;
      }
      if (array.Length == 3 && array[0].ToLower() == "override")
      {
        string text4 = array[1].ToLower();
        string text5 = array[2].ToLower();
        if (AnimationConstructor.AnimationLibrary.Instance.GetAnimation(text5, "override") == null)
        {
          PunkEmotesPlugin.Log.LogError("Override animation '" + text5 + "' not found.");
          return false;
        }
        if (emotesManagerByNetId.overrideAliases.ContainsKey(text4))
        {
          List<string> list = emotesManagerByNetId.overrideAliases[text4];
          string animationName = text5 + list[2];
          string animationName2 = text5 + list[3];
          emotesManagerByNetId.ApplyPunkOverrides("ALL", emotesManagerByNetId, animationName, list[0]);
          emotesManagerByNetId.ApplyPunkOverrides("ALL", emotesManagerByNetId, animationName2, list[1]);
        }
        else
        {
          emotesManagerByNetId.ApplyPunkOverrides("ALL", emotesManagerByNetId, text5, text4);
        }
        return false;
      }
      if (array.Length == 2)
      {
        string animationCategory = array[0].ToLower();
        string animationName3 = array[1].ToLower();
        emotesManagerByNetId.PlayAnimationClip("ALL", emotesManagerByNetId, animationName3, animationCategory);
        return false;
      }
      if (array.Length == 1)
      {
        string animationName4 = array[0].ToLower();
        emotesManagerByNetId.PlayAnimationClip("ALL", emotesManagerByNetId, animationName4);
        return false;
      }
      PunkEmotesPlugin.Log.LogWarning("Invalid emotes format. Expected '/em [category] [name]', '/em [name]', or '/em override [originOverride] [newOverride]'.");
      return false;
    }
    AnimationClip[] animationClips = emotesManagerByNetId._animator.runtimeAnimatorController.animationClips;
    if (animationClips != null && animationClips.Length != 0)
    {
      AnimationClip[] array2 = animationClips;
      foreach (AnimationClip val in array2)
      {
        PunkEmotesPlugin.Log.LogInfo("Overridable animation: " + (val).name);
      }
    }
    else
    {
      PunkEmotesPlugin.Log.LogWarning("No animation clips found in the Animator.");
    }
    return false;
  }

  //Typos in method name by Kisseff
  [HarmonyPatch(typeof(ChatBehaviour), nameof(ChatBehaviour.UserCode_Rpc_RecieveChatMessage__String__Boolean__ChatChannel))]
  [HarmonyPrefix]
  public static bool UserCode_Rpc_RecieveChatMessage_Prefix(string message, bool _isEmoteMessage, ChatBehaviour.ChatChannel _chatChannel)
  {
    PunkEmotesPlugin.Log.LogInfo(message);
    if (message.Contains("<>#PUNKEMOTES#"))
    {
      PunkEmotesPlugin.Log.LogInfo("PUNKEMOTES detected in RPC!");
      string[] array = message.Split('#');
      if (array.Length >= 4)
      {
        if (!uint.TryParse(array[2], out var result))
        {
          PunkEmotesPlugin.Log.LogWarning("Failed to parse netId from message: " + array[2]);
          return false;
        }
        Player playerByNetId = PlayerRegistry.GetPlayerByNetId(result);
        if (!(playerByNetId != null))
        {
          PunkEmotesPlugin.Log.LogWarning($"Player with netId '{result}' not found.");
          return false;
        }
        PunkEmotesManager component = playerByNetId.GetComponent<PunkEmotesManager>();
        if (component != null)
        {
          component.HandleChatAnimationMessage(message);
          return false;
        }
      }
      return false;
    }
    return true;
  }

  [HarmonyPatch(typeof(ChatBehaviour), nameof(ChatBehaviour.UserCode_Cmd_SendChatMessage__String__ChatChannel))]
  [HarmonyPrefix]
  public static bool UserCode_Cmd_SendChatMessage_Prefix(ChatBehaviour __instance, string _message, ChatBehaviour.ChatChannel _chatChannel)
  {
    if (_message.Contains("<>#PUNKEMOTES#"))
    {
      if (rpcMethod != null)
      {
        rpcMethod.Invoke(__instance, new object[3]
        {
            _message,
            true,
            (ChatBehaviour.ChatChannel)3
        });
        PunkEmotesPlugin.Log.LogInfo("Caught <>#PUNKEMOTES#, sent to RPC");
        return false;
      }
      return false;
    }
    return true;
  }
}
