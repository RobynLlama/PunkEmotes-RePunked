using HarmonyLib;
using PunkEmotes.Components;
using PunkEmotes.Internals;

namespace PunkEmotes.Patches;

internal static class Player_Patches
{
  [HarmonyPatch(typeof(Player), nameof(Player.Start))]
  [HarmonyPostfix]
  private static void Start_Postfix(Player __instance)
  {
    PunkEmotesManager punkEmotesManager = __instance.gameObject.GetComponent<PunkEmotesManager>();
    if (punkEmotesManager == null)
    {
      punkEmotesManager = __instance.gameObject.AddComponent<PunkEmotesManager>();
    }
    PlayerRegistry.RegisterPlayer(__instance, punkEmotesManager);
  }
}
