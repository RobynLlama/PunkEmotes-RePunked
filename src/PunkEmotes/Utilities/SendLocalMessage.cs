namespace PunkEmotes.Utils;

internal static partial class Utilities
{
  internal static void SendLocalMessage(string message)
  {
    if (!Player._mainPlayer) return;
    Player._mainPlayer._cB.New_ChatMessage(message);
  }
}
