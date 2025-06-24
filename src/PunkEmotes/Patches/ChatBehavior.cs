using System;
using HarmonyLib;
using PunkEmotes.Internals;

namespace PunkEmotes.Patches;

internal static class ChatBehaviour_Patches
{
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
}
