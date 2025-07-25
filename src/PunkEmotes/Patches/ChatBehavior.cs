using System;
using System.Text.RegularExpressions;
using HarmonyLib;
using PunkEmotes.Internals;

namespace PunkEmotes.Patches;

internal static class ChatBehaviour_Patches
{
  private static readonly PunkEmotesCommandDispatcher PunkDispatcher = new();

  public static string GetSanitizedMessageContents(string input)
  {

    var index = input.IndexOf(':');
    string result = index >= 0 ? input[(index + 1)..] : input;

    //remove all XML formatting of any type
    return Regex.Replace(result, @"<.*?>", string.Empty).Trim();
  }

  [HarmonyPatch(typeof(ChatBehaviour), nameof(ChatBehaviour.Cmd_SendChatMessage))]
  [HarmonyPrefix]
  private static bool Send_ChatMessage_Prefix(ref string _message, ChatBehaviour.ChatChannel _chatChannel, ChatBehaviour __instance)
  {
    if (string.IsNullOrEmpty(_message))
    {
      return true;
    }

    var contents = GetSanitizedMessageContents(_message);
    PunkEmotesPlugin.Log.LogInfo($"Message: {_message}\nContents: {contents}");

    if (!contents.StartsWith("/em ", StringComparison.OrdinalIgnoreCase))
    {
      return true;
    }

    var commandStr = contents.Replace("/em", string.Empty);
    PunkDispatcher.ParseAndRunCommand(commandStr);

    return false;
  }
}
