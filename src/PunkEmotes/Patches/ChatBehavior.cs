using System;
using HarmonyLib;
using PunkEmotes.Components;
using PunkEmotes.Internals;
using PunkEmotes.Utils;

namespace PunkEmotes.Patches;

[HarmonyPatch]
internal static class ChatBehaviour_Patches
{
  // Replace chat-based patches with direct RPC calls for animations and overrides
  // Patch for playing emotes - Triggered when an emote is played, either from command or remote player request
  // Robyn Note: ^ I don't think these original comments makes sense here but I preserved them
  [HarmonyPatch(typeof(ChatBehaviour), nameof(ChatBehaviour.Send_ChatMessage))]
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
      Utilities.SendLocalMessage("Main player is null! This is a really bad sign!");
      return true; // Allow default handling if something is wrong
    }

    if (AnimationConstructor.PunkEmotesLibrary.Instance == null)
    {
      Utilities.SendLocalMessage("PunkEmotesLibrary instance is null!");
      return true; // Avoid further execution if the library is unavailable
    }

    // Use PunkEmotesManager to handle animations locally
    PunkEmotesManager emotesManager = Player._mainPlayer.GetComponent<PunkEmotesManager>();
    if (emotesManager == null)
    {
      Utilities.SendLocalMessage("EmotesManager not found for player!");
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
          Utilities.SendLocalMessage("'/em overrides' is used to list override targets, use '/em override animation target' if this was a mistake");
          _message = string.Empty;
        }
        return false;

      case string command when string.Equals(command, "help", StringComparison.OrdinalIgnoreCase):
        if (parts.Length == 1 || string.IsNullOrWhiteSpace(parts[1]))
        {
          // Show help messages
          Utilities.SendLocalMessage("For information on emotes, use '/em help emotes'");
          Utilities.SendLocalMessage("For information on overrides, use '/em help overrides'");
          _message = string.Empty;
          return false;
        }
        else if (string.Equals(parts[1], "emotes", StringComparison.OrdinalIgnoreCase))
        {
          Utilities.SendLocalMessage("Use '/em [category] animation_name' to play an animation.");
          Utilities.SendLocalMessage("[category] is an optional sorting tag. For example, '/em sit imp' uses the first 'imp' animation in the 'sit' category.");
          _message = string.Empty;
          return false;
        }
        else if (string.Equals(parts[1], "overrides", StringComparison.OrdinalIgnoreCase))
        {
          Utilities.SendLocalMessage("Use '/em override animation_name target_name' to apply overrides.");
          Utilities.SendLocalMessage("animation_name is the name of the animation you want to use.");
          Utilities.SendLocalMessage("target_name is the name of the animation you want to replace.");
          Utilities.SendLocalMessage("To get a list of targets, use '/em overrides', but it's a big list! It'll clear your chat.");
          Utilities.SendLocalMessage("To undo an override, use '/em override delete target', or '/em override delete all' to reset to default.");
          _message = string.Empty;
          return false;
        }
        else
        {
          Utilities.SendLocalMessage($"Unrecognized help topic: {parts[1]}");
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
              Utilities.SendLocalMessage($"Animation {parts[2]} not found as {targetName} override!");
            }
          }
        }
        else
        {
          // Invalid format
          Utilities.SendLocalMessage("Invalid override command format!");
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
          Utilities.SendLocalMessage($"Animation {string.Join(' ', parts)} not found!");
        }
        _message = string.Empty;
        return false;
    }
  }

}
