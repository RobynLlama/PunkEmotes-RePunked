using System.Collections;
using Mirror;
using PunkEmotes.Components;
using UnityEngine;

namespace PunkEmotes.Internals;

public class PunkEmotesNetwork : NetworkBehaviour
{
  // Singleton instance
  public static PunkEmotesNetwork Instance { get; private set; }

  // Ensure only one instance exists and it's accessible
  private void Awake()
  {
    if (Instance != null && Instance != this)
    {
      PunkEmotesPlugin.Log.LogWarning("Found and destroyed duplicate PunkEmotesNetwork");
      Destroy(gameObject);
    }
    else
    {
      PunkEmotesPlugin.Log.LogInfo("Created PunkEmotesNetwork!");
      Instance = this;
      DontDestroyOnLoad(gameObject); // Keeps the instance across scenes
    }
  }

  public bool IsHostPlayer { get; private set; } = false;

  private void Start()
  {
    StartCoroutine(WaitForNetworkContext());
  }
  private IEnumerator WaitForNetworkContext()
  {
    // Wait until both isServer and isClient are set properly
    while (!NetworkServer.active)
    {
      yield return null; // Wait for the next frame
    }

    // Once both are set, determine host status
    IsHostPlayer = (Player._mainPlayer.isServer);
    PunkEmotesPlugin.Log.LogInfo($"Host Status determined after waiting: {IsHostPlayer}");
  }

  private static bool receivedModHandshakeResponse = false;

  // This method will be called to synchronize the animation state when a player joins
  [Command]
  public void Cmd_SyncRequest(NetworkConnection conn)
  {
    if (conn == null)
    {
      PunkEmotesPlugin.Log.LogError("CmdSyncRequest: connectionToClient is null!");
      return;
    }

    if (IsHostPlayer)
    {
      PunkEmotesPlugin.Log.LogInfo($"CmdSyncRequest received from connection: {conn}");
      Rpc_HandshakeResponse(conn, LCMPluginInfo.PLUGIN_VERSION);
      Rpc_SyncAnimationResponse(conn);
    }

    // Start a timeout check to see if the mod is installed and the server is responsive to our calls
    StartCoroutine(WaitForModHandshakeResponse(conn));
  }

  private IEnumerator WaitForModHandshakeResponse(NetworkConnection conn)
  {
    float timeoutDuration = 5.0f; // Timeout duration in seconds
    float timeElapsed = 0f;

    // Wait until the timeout duration or until we get the response
    while (timeElapsed < timeoutDuration)
    {
      if (receivedModHandshakeResponse)  // Flag to indicate if we received the response
      {
        yield break;  // Exit the coroutine if the response is received
      }

      timeElapsed += Time.deltaTime;
      yield return null;  // Wait for the next frame
    }

    // Timeout reached without a response
    PunkEmotesPlugin.SendLocalMessage("Server did not respond or does not have PunkEmotes installed. Custom emotes will likely not be broadcasted.");
  }

  // This method will send the animation sync response back to the requesting player
  [TargetRpc]
  public void Rpc_SyncAnimationResponse(NetworkConnection conn)
  {
    if (!IsHostPlayer)
    {
      PunkEmotesPlugin.Log.LogInfo($"SyncAnimationResponse detected!");
      // Iterate through all players in the PlayerRegistry, excluding the Local player
      foreach (var playerEntry in PlayerRegistry._playersByNetId.Values)
      {
        // Skip the Local player (the one who sent the sync request)
        if (playerEntry.PlayerInstance.netId != conn.identity.netId)
        {
          // Send the animation data for this player (Remote) to the Local player
          SendPlayerAnimationData(playerEntry.EmotesManager);

          // Set the response flag to true so that we don't get a timeout message
          receivedModHandshakeResponse = true;
        }
      }
    }
  }

  [TargetRpc]
  public void Rpc_HandshakeResponse(NetworkConnection conn, string version)
  {
    if (IsHostPlayer)
    {
      PunkEmotesPlugin.Log.LogInfo($"Sending Handshake Response to {conn} with version {version}");
      CheckModVersion(version);
    }
  }

  internal static void CheckModVersion(string version)
  {
    if (string.IsNullOrEmpty(version))
    {
      PunkEmotesPlugin.SendLocalMessage("Server's PunkEmotes version not detected, but plugin appears to be installed.");
    }
    else if (version != LCMPluginInfo.PLUGIN_VERSION)
    {
      PunkEmotesPlugin.SendLocalMessage($"PunkEmotes version mismatch: Your version ({LCMPluginInfo.PLUGIN_VERSION}) | Server version: ({version})");
    }
    else
    {
      PunkEmotesPlugin.SendLocalMessage($"PunkEmotes{LCMPluginInfo.PLUGIN_VERSION} detected on server! Have fun <3");
    }
  }

  private void SendPlayerAnimationData(PunkEmotesManager remoteEmotesManager)
  {
    PunkEmotesPlugin.Log.LogInfo($"Sending Player Animation Data to {remoteEmotesManager._player._nickname}");
    // Get the relevant animation data for this player's PunkEmotesManager
    remoteEmotesManager.GetPlayerAnimationState(remoteEmotesManager);  // Retrieve remote player's current animation state
    remoteEmotesManager.GetPlayerAnimationOverrides(remoteEmotesManager);  // Retrieve remote player's current overrides
  }

  // This method will be used to send animation updates to other players (only called by the host)
  [Command]
  public void Cmd_AnimationChange(PunkEmotesManager emotesManager, string animationName, string animationCategory = null)
  {
    if (IsHostPlayer)
    {
      PunkEmotesPlugin.Log.LogInfo($"Client sending animation change command from {emotesManager._player._nickname}: ({animationName}, {animationCategory})");
      // Send animation update request to the host
      Rpc_SendAnimationUpdate(emotesManager, animationName, animationCategory);
    }
  }

  // This method will handle receiving an animation update from another player
  [ClientRpc]
  public void Rpc_SendAnimationUpdate(PunkEmotesManager remoteEmotesManager, string animationName, string animationCategory = null)
  {
    PunkEmotesPlugin.Log.LogInfo($"Server sending animation update response from {remoteEmotesManager._player._nickname}: ({animationName}, {animationCategory})");
    // Apply the received animation update to the local player's emotes manager
    if (!IsHostPlayer)
    {
      remoteEmotesManager.PlayAnimationClip(remoteEmotesManager, animationName, animationCategory);
    }
  }

  [Command]
  public void Cmd_StopAnimation(PunkEmotesManager emotesManager)
  {
    PunkEmotesPlugin.Log.LogInfo($"Client sending stop animation command from {emotesManager._player._nickname}");
    if (IsHostPlayer)
    {
      Rpc_StopAnimation(emotesManager);
    }
  }

  [ClientRpc]
  public void Rpc_StopAnimation(PunkEmotesManager remoteEmotesManager)
  {
    PunkEmotesPlugin.Log.LogInfo($"Server sending stop animation response from {remoteEmotesManager._player._nickname}");
    if (!IsHostPlayer)
    {
      remoteEmotesManager.StopAnimation(remoteEmotesManager);
    }
  }

  // This method will send animation override updates (only called by the host)
  [Command]
  public void Cmd_OverrideChange(PunkEmotesManager emotesManager, string overrideAnimation, string overrideTarget)
  {
    PunkEmotesPlugin.Log.LogInfo($"Client sending override change command from {emotesManager._player._nickname}: ({overrideAnimation}, {overrideTarget})");
    if (IsHostPlayer)
    {
      // Logic to send override updates to all players except the host
      Rpc_SendOverrideUpdate(emotesManager, overrideAnimation, overrideTarget);
    }
  }

  // This method will handle receiving an override update (for clients)
  [ClientRpc]
  public void Rpc_SendOverrideUpdate(PunkEmotesManager remoteEmotesManager, string overrideAnimation, string overrideTarget)
  {
    PunkEmotesPlugin.Log.LogInfo($"Server sending override update from {remoteEmotesManager._player._nickname}");
    // Apply the received override update to the local player's emotes manager
    if (!IsHostPlayer)
    {
      remoteEmotesManager.ApplyPunkOverrides(remoteEmotesManager, overrideAnimation, overrideTarget);
    }
  }

  [Command]
  public void Cmd_ClearOverrides(PunkEmotesManager emotesManager, string overrideTarget)
  {
    PunkEmotesPlugin.Log.LogInfo($"Client sending remove override command from {emotesManager._player._nickname}");
    if (IsHostPlayer)
    {
      Rpc_ClearOverrides(emotesManager, overrideTarget);
    }
  }

  [ClientRpc]
  public void Rpc_ClearOverrides(PunkEmotesManager remoteEmotesManager, string overrideTarget)
  {
    PunkEmotesPlugin.Log.LogInfo($"Server sending remove override response from {remoteEmotesManager._player._nickname}");
    if (!IsHostPlayer)
    {
      remoteEmotesManager.RemoveOverride(remoteEmotesManager, overrideTarget);
    }
  }
}
