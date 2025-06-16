using HarmonyLib;
using PunkEmotes.Components;

namespace PunkEmotes.Patches;

[HarmonyPatch]
internal static class PlayerMove_Patches
{
  // Patch for stopping animation when the player moves
  [HarmonyPatch(typeof(PlayerMove), nameof(PlayerMove.Set_MovementAction))]
  public class SetMovementActionPatch
  {
    static void Prefix(PlayerMove __instance, MovementAction _mA)
    {
      var player = __instance.gameObject.GetComponent<Player>();
      if (player == null) return;

      var emotesManager = player.GetComponent<PunkEmotesManager>();
      if (emotesManager == null) return;

      if (_mA != MovementAction.IDLE && emotesManager._isAnimationPlaying)
      {
        // Stop the animation if moving
        emotesManager.StopAnimation(emotesManager);
      }
    }
  }
}
