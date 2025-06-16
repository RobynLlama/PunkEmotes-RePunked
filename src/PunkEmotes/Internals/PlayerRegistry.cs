using System.Collections.Generic;
using PunkEmotes.Components;

namespace PunkEmotes.Internals;

public static class PlayerRegistry
{
  public static Dictionary<uint, PlayerEntry> _playersByNetId = new Dictionary<uint, PlayerEntry>();

  public class PlayerEntry
  {
    public string Nickname { get; set; }
    public Player PlayerInstance { get; set; }
    public PunkEmotesManager EmotesManager { get; set; }
  }

  // Register a player and their PunkEmotesManager, called by the host
  public static void RegisterPlayer(uint netId, string nickname, Player player, PunkEmotesManager emotesManager)
  {
    if (player != null && emotesManager != null && !_playersByNetId.ContainsKey(netId))
    {
      _playersByNetId[netId] = new PlayerEntry
      {
        Nickname = nickname,
        PlayerInstance = player,
        EmotesManager = emotesManager
      };
    }
  }

  // Unregister a player and their PunkEmotesManager, called by the host when player disconnects
  public static void UnregisterPlayer(uint netId)
  {
    _playersByNetId.Remove(netId);
  }

  // Clear all players from the registry (useful for cleanup on level reset or server shutdown)
  public static void ClearRegistry()
  {
    _playersByNetId.Clear();
  }

  // Get a player's PunkEmotesManager by their netId
  public static PunkEmotesManager GetEmotesManagerByNetId(uint netId)
  {
    return _playersByNetId.TryGetValue(netId, out var entry) ? entry.EmotesManager : null;
  }

  // Get a player instance by their netId
  public static Player GetPlayerByNetId(uint netId)
  {
    return _playersByNetId.TryGetValue(netId, out var entry) ? entry.PlayerInstance : null;
  }

  // Get a player instance by their nickname
  public static Player GetPlayerByNickname(string nickname)
  {
    foreach (var entry in _playersByNetId.Values)
    {
      if (entry.Nickname == nickname)
      {
        return entry.PlayerInstance;
      }
    }
    return null;
  }
}
