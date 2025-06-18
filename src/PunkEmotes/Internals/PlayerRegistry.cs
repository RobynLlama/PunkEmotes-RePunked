using System.Collections.Generic;
using PunkEmotes.Components;

namespace PunkEmotes.Internals;

public static class PlayerRegistry
{
  private class PlayerEntry
  {
    public string? Nickname { get; set; }

    public Player? PlayerInstance { get; set; }

    public PunkEmotesManager? EmotesManager { get; set; }
  }

  private static Dictionary<uint, PlayerEntry> _playersByNetId = new Dictionary<uint, PlayerEntry>();

  public static void RegisterPlayer(Player player, PunkEmotesManager emotesManager)
  {
    if (player != null && emotesManager != null)
    {
      uint netId = player.netId;
      if (!_playersByNetId.ContainsKey(netId))
      {
        _playersByNetId[netId] = new PlayerEntry
        {
          Nickname = player.Network_nickname,
          PlayerInstance = player,
          EmotesManager = emotesManager
        };
      }
    }
  }

  public static void UnregisterPlayer(Player player)
  {
    if (player != null)
    {
      uint netId = player.netId;
      _playersByNetId.Remove(netId);
    }
  }

  public static void ClearRegistry()
  {
    _playersByNetId.Clear();
  }

  public static PunkEmotesManager? GetEmotesManagerByNetId(uint netId)
  {
    PlayerEntry value;
    return _playersByNetId.TryGetValue(netId, out value) ? value.EmotesManager : null;
  }

  public static Player? GetPlayerByNetId(uint netId)
  {
    PlayerEntry value;
    return _playersByNetId.TryGetValue(netId, out value) ? value.PlayerInstance : null;
  }

  public static Player? GetPlayerByNickname(string nickname)
  {
    foreach (PlayerEntry value in _playersByNetId.Values)
    {
      if (value.Nickname == nickname)
      {
        return value.PlayerInstance;
      }
    }
    return null;
  }
}
