using HarmonyLib;

namespace PunkEmotes.Patches;

internal static class CharacterSelectManager_Patches
{
  [HarmonyPatch(typeof(CharacterSelectManager), "Select_CharacterFile")]
  [HarmonyPostfix]
  private static void Select_CharacterFile_Postfix()
  {
    PunkEmotesPlugin.AnimationConstructor.raceAnimatorReset = true;
    PunkEmotesPlugin.PlayerRegistry.ClearRegistry();
  }
}
