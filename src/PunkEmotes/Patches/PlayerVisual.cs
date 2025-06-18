using HarmonyLib;
using PunkEmotes.Internals;

namespace PunkEmotes.Patches;

internal static class PlayerVisual_Patches
{

  [HarmonyPatch(typeof(PlayerVisual), nameof(PlayerVisual.Iterate_AnimationCallback))]
  [HarmonyPostfix]
  private static void Iterate_AnimationCallback_Postfix(PlayerVisual __instance, ref string _animName, ref float _animLayer)
  {
    if (AnimationConstructor.raceAnimatorReset)
    {
      AnimationConstructor.LoadRaceFBXs();
    }
  }
}
