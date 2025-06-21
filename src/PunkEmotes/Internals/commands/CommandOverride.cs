using System.Collections.Generic;
using PunkEmotes.Components;
using SimpleCommandLib;

namespace PunkEmotes.Internals;

internal class CommandOverride : ICommandRunner
{
  public string CommandName => "override";

  public string CommandUsage => string.Empty;

  public bool Execute(string[] args)
  {
    if (args.Length < 2)
      return false;

    if (PlayerRegistry.GetEmotesManagerByNetId(Player._mainPlayer.netId) is not PunkEmotesManager emotesManagerByNetId)
    {
      PunkEmotesPlugin.Log.LogWarning($"Unable to get emotes manager for client ID (ChatBehavior): {Player._mainPlayer.netId}");
      return false;
    }

    string originName = args[0].ToLower();
    string overrideName = args[1].ToLower();
    if (AnimationConstructor.AnimationLibrary.Instance.GetAnimation(overrideName, "override") == null)
    {
      PunkEmotesPlugin.Log.LogError("Override animation '" + overrideName + "' not found.");
      return false;
    }

    if (emotesManagerByNetId.overrideAliases.ContainsKey(originName))
    {
      List<string> list = emotesManagerByNetId.overrideAliases[originName];
      string animationName = overrideName + list[2];
      string animationName2 = overrideName + list[3];
      emotesManagerByNetId.ApplyPunkOverrides("ALL", emotesManagerByNetId, animationName, list[0]);
      emotesManagerByNetId.ApplyPunkOverrides("ALL", emotesManagerByNetId, animationName2, list[1]);
    }
    else
    {
      emotesManagerByNetId.ApplyPunkOverrides("ALL", emotesManagerByNetId, overrideName, originName);
    }

    return true;
  }
}
