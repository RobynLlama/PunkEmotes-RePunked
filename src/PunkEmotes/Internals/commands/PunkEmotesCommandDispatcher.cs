using System;
using System.Collections.Generic;
using System.Linq;
using SimpleCommandLib;

namespace PunkEmotes.Internals;

internal class PunkEmotesCommandDispatcher : CommandDispatcher
{
  protected override Dictionary<string, ICommandRunner> CommandsMap { get => _commands; set { return; } }
  protected ICommandRunner EmotesCommand;
  private readonly Dictionary<string, ICommandRunner> _commands = new(StringComparer.InvariantCultureIgnoreCase);

  public PunkEmotesCommandDispatcher()
  {
    EmotesCommand = new CommandPlayEmote();
    TryAddCommand(new CommandHelp());
    TryAddCommand(new CommandOverride());
    TryAddCommand(new CommandList());
  }

  public override void OnCommandNotFound(string commandName) { }
  public override bool RunCommand(string commandName, string[] args)
  {
    if (base.RunCommand(commandName, args))
      return true;

    string[] emoteArgs = [.. (new[] { commandName.ToLowerInvariant() }).Concat(args).Select(s => s.ToLowerInvariant())];
    PunkEmotesPlugin.Log.LogDebug($"Running em with {emoteArgs.Length} args"); ;

    return EmotesCommand.Execute(emoteArgs);
  }
}
