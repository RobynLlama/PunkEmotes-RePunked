using HarmonyLib;

namespace PunkEmotes.Patches;

internal static class Player_Patches
{
  [HarmonyPatch(typeof(Player), "Start")]
  [HarmonyPostfix]
  private static void Postfix(Player __instance)
  {
    PunkEmotesPlugin.PunkEmotesManager punkEmotesManager = __instance.gameObject.GetComponent<PunkEmotesPlugin.PunkEmotesManager>();
    if (punkEmotesManager == null)
    {
      punkEmotesManager = __instance.gameObject.AddComponent<PunkEmotesPlugin.PunkEmotesManager>();
    }
    PunkEmotesPlugin.PlayerRegistry.RegisterPlayer(__instance, punkEmotesManager);
  }
}
