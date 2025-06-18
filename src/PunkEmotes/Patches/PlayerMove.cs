using HarmonyLib;
using PunkEmotes.Components;

namespace PunkEmotes.Patches;

internal static class PlayerMove_Patches
{
  [HarmonyPatch(typeof(PlayerMove), "Set_MovementAction")]
  [HarmonyPostfix]
  private static void Set_MovementAction_Postfix(PlayerMove __instance, MovementAction _mA)
  {
    Player player = __instance.gameObject.GetComponent<Player>();
    if (!(player == null))
    {
      PunkEmotesManager emotesManager = player.GetComponent<PunkEmotesManager>();
      if (!(emotesManager == null) && _mA != MovementAction.IDLE && emotesManager._isAnimationPlaying)
      {
        emotesManager.StopAnimation(emotesManager);
      }
    }
  }
}
