using System;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using CodeTalker.Networking;
using HarmonyLib;
using PunkEmotes.Components;
using PunkEmotes.Internals;
using PunkEmotes.Patches;

namespace PunkEmotes;

[BepInPlugin(LCMPluginInfo.PLUGIN_GUID, LCMPluginInfo.PLUGIN_NAME, LCMPluginInfo.PLUGIN_VERSION)]
[BepInProcess("ATLYSS.exe")]
public class PunkEmotesPlugin : BaseUnityPlugin
{
	internal static ManualLogSource Log = null!;

	private void Awake()
	{
		Log = Logger;
		Log.LogInfo("Punk Emotes is rockin'!");
		Harmony patcher = new("punkalyn.punkemotes");
		int patchCount = 5;
		try
		{
			patcher.PatchAll(typeof(CharacterSelectManager_Patches));
			patcher.PatchAll(typeof(ChatBehaviour_Patches));
			patcher.PatchAll(typeof(Player_Patches));
			patcher.PatchAll(typeof(PlayerMove_Patches));
			patcher.PatchAll(typeof(PlayerVisual_Patches));
			if (patchCount != patcher.GetPatchedMethods().Count())
			{
				Log.LogError($"Punk Emotes patched {patcher.GetPatchedMethods().Count()} methods out of {patchCount} intended patches!");
			}

			CodeTalkerNetwork.RegisterListener<PunkAnimationPacket>(PunkEmotesManager.HandleChatAnimationMessage);
		}
		catch (Exception ex)
		{
			Log.LogError("Exception caught while patching: " + ex.Message);
		}
	}

	internal static void SendChatMessage(string message)
	{
		if (Player._mainPlayer)
			Player._mainPlayer._cB.New_ChatMessage(message);
	}
}
