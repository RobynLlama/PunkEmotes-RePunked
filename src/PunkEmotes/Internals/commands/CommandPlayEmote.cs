using PunkEmotes.Components;
using SimpleCommandLib;

namespace PunkEmotes.Internals;

internal class CommandPlayEmote : ICommandRunner
{
  public string CommandName => "emote";

  public string CommandUsage => string.Empty;

  public bool Execute(string[] args)
  {
    if (args.Length == 0)
      return false;

    if (PlayerRegistry.GetEmotesManagerByNetId(Player._mainPlayer.netId) is not PunkEmotesManager emotesManagerByNetId)
    {
      PunkEmotesPlugin.Log.LogWarning($"Unable to get emotes manager for client ID (ChatBehavior): {Player._mainPlayer.netId}");
      return false;
    }

    string? animationCategory = args.Length > 1 ? args[0] : null;
    string animationName = args.Length == 1 ? args[0] : args[1];

    emotesManagerByNetId.PlayAnimationClip("ALL", emotesManagerByNetId, animationName, animationCategory);

    return true;
  }
}
