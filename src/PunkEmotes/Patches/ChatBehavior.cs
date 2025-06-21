using System;
using System.Reflection;
using HarmonyLib;
using PunkEmotes.Components;
using PunkEmotes.Internals;

namespace PunkEmotes.Patches;

internal static class ChatBehaviour_Patches
{
  private static MethodInfo rpcMethod = typeof(ChatBehaviour).GetMethod("Rpc_RecieveChatMessage", BindingFlags.Instance | BindingFlags.NonPublic);
  private static readonly PunkEmotesCommandDispatcher PunkDispatcher = new();

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

    var commandStr = _message.Replace("/em", string.Empty);
    PunkDispatcher.ParseAndRunCommand(commandStr);

    return false;
  }

  //Typos in method name by Kisseff
  [HarmonyPatch(typeof(ChatBehaviour), nameof(ChatBehaviour.UserCode_Rpc_RecieveChatMessage__String__Boolean__ChatChannel))]
  [HarmonyPrefix]
  public static bool UserCode_Rpc_RecieveChatMessage_Prefix(string message, bool _isEmoteMessage, ChatBehaviour.ChatChannel _chatChannel)
  {
    if (message.Contains(PunkEmotesManager.PUNK_NETWORK_SIGNATURE_DIRTY))
    {
      PunkEmotesPlugin.Log.LogInfo("PUNKEMOTES detected in RPC!");
      PunkEmotesPlugin.Log.LogMessage($"PunkNetwork Received: {message}");
      if (PunkNetworkPacket.TryFromString(message, out var packet))
      {
        if (PlayerRegistry.GetPlayerByNetId(packet.SenderNetworkID) is Player sender)
        {
          if (sender.GetComponent<PunkEmotesManager>() is PunkEmotesManager senderManager)
          {
            senderManager.HandleChatAnimationMessage(packet);
          }
          else
            PunkEmotesPlugin.Log.LogWarning($"Unable to fetch PunkEmotesManager from player {sender._nickname}");
        }
        else
          PunkEmotesPlugin.Log.LogWarning($"Unable to locate player by NetID (sender): {packet.SenderNetworkID}");
      }

      //Always return false here since we identified a PUNKNETWORK message
      return false;
    }
    return true;
  }

  [HarmonyPatch(typeof(ChatBehaviour), nameof(ChatBehaviour.UserCode_Cmd_SendChatMessage__String__ChatChannel))]
  [HarmonyPrefix]
  public static bool UserCode_Cmd_SendChatMessage_Prefix(ChatBehaviour __instance, string _message, ChatBehaviour.ChatChannel _chatChannel)
  {
    if (_message.Contains(PunkEmotesManager.PUNK_NETWORK_SIGNATURE_DIRTY))
    {
      if (rpcMethod != null)
      {
        rpcMethod.Invoke(__instance,
        [
            _message,
            true,
            PunkEmotesManager.PUNK_NETWORK_CHANNEL,
        ]);
        PunkEmotesPlugin.Log.LogInfo("Caught network signature, sent to RPC");
        return false;
      }
      return false;
    }
    return true;
  }
}
