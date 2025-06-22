using SimpleCommandLib;

namespace PunkEmotes.Internals;

internal class CommandHelp : ICommandRunner
{
  public string CommandName => "help";

  public string CommandUsage => string.Empty;

  public bool Execute(string[] args)
  {
    PunkEmotesPlugin.SendChatMessage("Commands: '/em animation_name (or race)'");
    PunkEmotesPlugin.SendChatMessage("Commands: '/em category animation_name (or race)'");
    PunkEmotesPlugin.SendChatMessage("Categories: 'sit', 'dance'");
    PunkEmotesPlugin.SendChatMessage("Test animation: '/em 02'");
    PunkEmotesPlugin.Log.LogInfo("Available commands: overrides, list, help");
    return true;
  }
}
