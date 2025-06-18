using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using PunkEmotes.Components;
using PunkEmotes.Patches;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PunkEmotes;

[BepInPlugin(LCMPluginInfo.PLUGIN_GUID, LCMPluginInfo.PLUGIN_NAME, LCMPluginInfo.PLUGIN_VERSION)]
[BepInProcess("ATLYSS.exe")]
public class PunkEmotesPlugin : BaseUnityPlugin
{
	public static class PlayerRegistry
	{
		private class PlayerEntry
		{
			public string Nickname { get; set; }

			public Player PlayerInstance { get; set; }

			public PunkEmotesManager EmotesManager { get; set; }
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

		public static PunkEmotesManager GetEmotesManagerByNetId(uint netId)
		{
			PlayerEntry value;
			return _playersByNetId.TryGetValue(netId, out value) ? value.EmotesManager : null;
		}

		public static Player GetPlayerByNetId(uint netId)
		{
			PlayerEntry value;
			return _playersByNetId.TryGetValue(netId, out value) ? value.PlayerInstance : null;
		}

		public static Player GetPlayerByNickname(string nickname)
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

	internal static ManualLogSource Logger;

	public static bool logInfoEnabled = false;

	public static bool logWarningEnabled = true;

	public static bool logErrorEnabled = true;

	private static bool logMethod = true;

	private void Awake()
	{
		Logger = base.Logger;
		LogInfo("Punk Emotes is rockin'!");
		Harmony patcher = new("punkalyn.punkemotes");
		int patchCount = 7;
		try
		{
			patcher.PatchAll(typeof(CharacterSelectManager_Patches));
			patcher.PatchAll(typeof(ChatBehaviour_Patches));
			patcher.PatchAll(typeof(Player_Patches));
			patcher.PatchAll(typeof(PlayerMove_Patches));
			patcher.PatchAll(typeof(PlayerVisual_Patches));
			if (patchCount != patcher.GetPatchedMethods().Count())
			{
				LogError($"Punk Emotes patched {patcher.GetPatchedMethods().Count()} methods out of {patchCount} intended patches!");
			}
		}
		catch (Exception ex)
		{
			LogError("Exception caught while patching: " + ex.Message);
		}
	}

	public static void LogMethod(bool enable)
	{
		logMethod = enable;
	}

	public static void LogInfo(string message, bool? shouldLog = null)
	{
		if ((shouldLog ?? logInfoEnabled) && logMethod)
		{
			Logger.LogInfo(message);
		}
	}

	public static void LogWarning(string message, bool? shouldLog = null)
	{
		if ((shouldLog ?? logWarningEnabled) && logMethod)
		{
			Logger.LogWarning(message);
		}
	}

	public static void LogError(string message, bool? shouldLog = null)
	{
		if ((shouldLog ?? logErrorEnabled) && logMethod)
		{
			Logger.LogError(message);
		}
	}

	internal static void SendChatMessage(string message)
	{
		if (Player._mainPlayer)
			Player._mainPlayer._cB.New_ChatMessage(message);
	}
}
