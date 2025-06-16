using HarmonyLib;
using PunkEmotes.Internals;

namespace PunkEmotes.Patches;

[HarmonyPatch]
internal static class CharacterSelectManager_Patches
{
  [HarmonyPatch(typeof(CharacterSelectManager), nameof(CharacterSelectManager.Select_CharacterFile))]
  class ResetCache
  {
    static void Postfix()
    {
      AnimationConstructor.raceAnimatorReset = true;
      PlayerRegistry.ClearRegistry();
      //LogInfo("Reset call for animators");
    }
  }
}
