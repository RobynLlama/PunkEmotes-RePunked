using System;
using System.Diagnostics.CodeAnalysis;
using CodeTalker.Packets;
using Newtonsoft.Json;

namespace PunkEmotes.Internals;

/// <summary>
/// Represents one packet of information as sent/received
/// by PunkNetwork
/// </summary>
internal class PunkAnimationPacket : PacketBase
{

  public override string PacketSourceGUID => LCMPluginInfo.PLUGIN_GUID;

  /// <summary>
  /// Network ID of the client that sent this request
  /// </summary>
  [JsonProperty]
  public readonly uint SenderNetworkID;

  /// <summary>
  /// Whether the target frame was set to "ALL"
  /// </summary>
  [JsonProperty]
  public readonly bool TargetAll = false;

  /// <summary>
  /// If the target frame was not "ALL" this will
  /// contain the target player's network ID
  /// </summary>
  [JsonProperty]
  public readonly uint? MessageTargetNetworkID;

  /// <summary>
  /// TOOD: make this an enum.
  /// Valid values:
  ///   START
  ///   STOP
  ///   SYNCREQUEST
  ///   OVERRIDE
  /// </summary>
  [JsonProperty]
  public readonly string RequestType;

  /// <summary>
  /// The name of the animation is the being requested
  /// </summary>
  [JsonProperty]
  public readonly string AnimationName;

  /// <summary>
  /// The category of the animation being requested
  /// </summary>
  [JsonProperty]
  public readonly string AnimationCategory;

  [JsonConstructor]
  internal PunkAnimationPacket(uint SenderNetworkID, bool TargetAll,
  uint? MessageTargetNetworkID, string RequestType,
  string AnimationName, string AnimationCategory)
  {
    this.SenderNetworkID = SenderNetworkID;
    this.TargetAll = TargetAll;
    this.MessageTargetNetworkID = MessageTargetNetworkID;
    this.RequestType = RequestType;
    this.AnimationName = AnimationName;
    this.AnimationCategory = AnimationCategory;
  }

  public PunkAnimationPacket(uint sender, string targetInfo,
  string requestType, string animationName, string animationCategory)
  {
    SenderNetworkID = sender;

    if (targetInfo.Equals("ALL", StringComparison.InvariantCultureIgnoreCase))
      TargetAll = true;
    else if (uint.TryParse(targetInfo, out var targetID))
      MessageTargetNetworkID = targetID;
    else
      throw new ArgumentException($"{nameof(targetInfo)} must either be ALL or network ID of target, message is malformed!\nInput: {targetInfo}");

    RequestType = requestType;
    AnimationName = animationName;
    AnimationCategory = animationCategory;
  }

  [Obsolete("PunkEmotes no longer supports using strings to create packets please use the common constructor instead")]
  public static bool TryFromString(string message, [NotNullWhen(true)] out PunkAnimationPacket? result)
  {
    result = null;

    string[] array = message.ToLower().Split('#', StringSplitOptions.None);

    if (array.Length < 5)
    {
      PunkEmotesPlugin.Log.LogWarning($"Attempted to parse malformed network message (Block count): {message}");
      return false;
    }

    if (uint.TryParse(array[2], out var netID))
    {
      string target = array[3];
      string requestType = array[4];
      string aniName = array[5];
      string aniCat = array.Length > 6 ? array[6] : string.Empty;

      try
      {
        result = new(netID, target, requestType, aniName, aniCat);
        return true;
      }
      catch (ArgumentException ex)
      {
        PunkEmotesPlugin.Log.LogError($"Error while parsing: \n{ex}\n");
      }

      return false;
    }

    PunkEmotesPlugin.Log.LogWarning($"Attempted to parse malformed network message (SenderID): {message}");
    return false;
  }

  public string SerializeToString()
  {
    return base.ToString();
  }
}
