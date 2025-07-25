using HarmonyLib;
using PunkEmotes.Components;
using PunkEmotes.Internals;

namespace PunkEmotes.Patches;

internal static class Player_Patches
{
  [HarmonyPatch(typeof(Player), nameof(Player.OnStartAuthority))]
  [HarmonyPostfix]
  private static void Start_Postfix(Player __instance)
  {
    PunkEmotesManager punkEmotesManager = __instance.gameObject.GetComponent<PunkEmotesManager>() ?? __instance.gameObject.AddComponent<PunkEmotesManager>();
    PlayerRegistry.RegisterPlayer(__instance, punkEmotesManager);
  }
}
