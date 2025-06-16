using HarmonyLib;
using PunkEmotes.Components;
using PunkEmotes.Internals;

namespace PunkEmotes.Patches;

[HarmonyPatch]
internal static class Player_Patches
{
  // Attach PunkEmotesManager to player and register them when they spawn
  [HarmonyPatch(typeof(Player), nameof(Player.OnStartAuthority))]
  static void Postfix(Player __instance)
  {
    var emotesManager = __instance.gameObject.GetComponent<PunkEmotesManager>();
    if (emotesManager == null)
    {
      // Attach the PunkEmotesManager if not already attached
      emotesManager = __instance.gameObject.AddComponent<PunkEmotesManager>();
    }

    var punkEmotesNetwork = Player._mainPlayer.GetComponent<PunkEmotesNetwork>();
    if (punkEmotesNetwork == null)
    {
      punkEmotesNetwork = __instance.gameObject.AddComponent<PunkEmotesNetwork>();
    }

    if (__instance._isHostPlayer)
    {
      // Register player with the PlayerRegistry
      PlayerRegistry.RegisterPlayer(__instance.netId, __instance._nickname, __instance, emotesManager);
    }
  }
}
