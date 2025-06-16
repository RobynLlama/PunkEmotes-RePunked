using HarmonyLib;
using PunkEmotes.Internals;

namespace PunkEmotes.Patches;

[HarmonyPatch]
internal static class PlayerVisual_Patches
{
  [HarmonyPatch(typeof(PlayerVisual), nameof(PlayerVisual.Iterate_AnimationCallback))]
  public class LoadFBX
  {
    //Robyn Note: __instance and ref arguments are never used here
    static void Postfix(PlayerVisual __instance, ref string _animName, ref float _animLayer)
    {
      if (AnimationConstructor.raceAnimatorReset == true)
      {
        AnimationConstructor.LoadRaceFBXs();
      }
    }
  }
}
