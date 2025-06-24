using System.Text;
using SimpleCommandLib;

namespace PunkEmotes.Internals;

internal class CommandList : ICommandRunner
{
  public string CommandName => "list";

  public string CommandUsage => string.Empty;

  public bool Execute(string[] args)
  {
    var sbc = new StringBuilder("All Animations:\n\n");

    foreach (var category in AnimationConstructor.AnimationLibrary.Instance.animationClips)
    {
      sbc.Append(category.Key);
      sbc.AppendLine(":");

      foreach (var item in category.Value)
      {
        sbc.Append("  ");
        sbc.Append(item.Key);
        sbc.Append(": ");
        sbc.AppendLine(item.Value.name);
      }

      sbc.Append('\n');
    }

    PunkEmotesPlugin.Log.LogDebug(sbc.ToString());
    return true;
  }
}
