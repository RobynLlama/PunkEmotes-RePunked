using HarmonyLib;
using PunkEmotes.Internals;

namespace PunkEmotes.Patches;

internal static class CharacterSelectManager_Patches
{
  [HarmonyPatch(typeof(CharacterSelectManager), nameof(CharacterSelectManager.Select_CharacterFile))]
  [HarmonyPostfix]
  private static void Select_CharacterFile_Postfix()
  {
    AnimationConstructor.raceAnimatorReset = true;
    PlayerRegistry.ClearRegistry();
  }
}
