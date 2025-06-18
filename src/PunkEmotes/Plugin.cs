using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Mirror;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PunkEmotes;

[BepInPlugin("punkalyn.punkemotes", "PunkEmotes", "0.1.0")]
[BepInProcess("ATLYSS.exe")]
public class PunkEmotesPlugin : BaseUnityPlugin
{
	public class AnimationConstructor
	{
		[HarmonyPatch(typeof(CharacterSelectManager), "Select_CharacterFile")]
		private class ResetCache
		{
			private static void Postfix()
			{
				raceAnimatorReset = true;
				PlayerRegistry.ClearRegistry();
			}
		}

		[HarmonyPatch(typeof(PlayerVisual), "Iterate_AnimationCallback")]
		public class LoadFBX
		{
			private static void Postfix(PlayerVisual __instance, ref string _animName, ref float _animLayer)
			{
				if (raceAnimatorReset)
				{
					LoadRaceFBXs();
				}
			}
		}

		public class AnimationLibrary
		{
			private static AnimationLibrary _instance;

			private Dictionary<string, Dictionary<string, AnimationClip>> animationClips = new Dictionary<string, Dictionary<string, AnimationClip>>
			{
				{
					"dance",
					new Dictionary<string, AnimationClip>()
				},
				{
					"general",
					new Dictionary<string, AnimationClip>()
				},
				{
					"override",
					new Dictionary<string, AnimationClip>()
				},
				{
					"sit",
					new Dictionary<string, AnimationClip>()
				}
			};

			public static AnimationLibrary Instance
			{
				get
				{
					if (_instance == null)
					{
						_instance = new AnimationLibrary();
					}
					return _instance;
				}
			}

			private AnimationLibrary()
			{
			}

			public void PopulateDefaultAnimations()
			{
				foreach (KeyValuePair<string, Animator> raceAnimator in raceAnimators)
				{
					string key = raceAnimator.Key;
					Animator value = raceAnimator.Value;
					AnimationClip[] array = ExtractAnimationsFromAnimator(value);
					AnimationClip[] array2 = array;
					foreach (AnimationClip clip in array2)
					{
						if (clip == null)
						{
							continue;
						}
						if (clip.name.Contains("dance"))
						{
							if (clip.name == "Kobold_dance")
								clip.name = "kubold_dance";

							animationClips["dance"][key + "_dance"] = clip;
							LogInfo("Added " + clip.name + " as " + key + "_dance to animation library!");
						}
						if (clip.name.Contains("sitInit") && !clip.name.Contains("02"))
						{
							if (clip.name == "Kobold_sitInit")
							{
								clip.name = "kubold_sitInit";
							}
							animationClips["override"][key + "_sitInit"] = clip;
							animationClips["sit"][key + "_sitInit"] = clip;
							LogInfo("Added " + clip.name + " as " + key + "_sitInit to animation library!");
						}
						if (clip.name.Contains("sitLoop") && !clip.name.Contains("02"))
						{
							if (clip.name == "Kobold_sitLoop")
							{
								clip.name = "kubold_sitLoop";
							}
							animationClips["override"][key + "_sitLoop"] = clip;
							LogInfo("Added " + clip.name + " as " + key + "_sitLoop to animation library!");
						}
						if (clip.name.Contains("sitInit02"))
						{
							if (clip.name == "Kobold_sitInit02")
							{
								clip.name = "kubold_sitInit02";
							}
							animationClips["override"][key + "_sitInit02"] = clip;
							LogInfo("Added " + clip.name + " as " + key + "_sitInit02 to animation library!");
						}
						if (clip.name.Contains("sitLoop02"))
						{
							if (clip.name == "Kobold_sitLoop")
							{
								clip.name = "kubold_sitLoop";
							}
							animationClips["override"][key + "_sitLoop02"] = clip;
							LogInfo("Added " + clip.name + " as " + key + "_sitLoop02 to animation library!");
						}
					}
				}
			}

			private AnimationClip[] ExtractAnimationsFromAnimator(Animator animator)
			{
				List<AnimationClip> list = new List<AnimationClip>();
				AnimationClip[] array = animator.runtimeAnimatorController.animationClips;
				foreach (AnimationClip item in array)
				{
					list.Add(item);
				}
				return list.ToArray();
			}

			public void LoadAnimations()
			{
				AnimationClip[] array = Resources.LoadAll<AnimationClip>("Animations/");
				AnimationClip[] array2 = array;
				foreach (AnimationClip val in array2)
				{
				}
			}

			public AnimationClip GetAnimation(string name, string category)
			{
				name = name.ToLowerInvariant();
				if (!string.IsNullOrEmpty(category) && animationClips.ContainsKey(category))
				{
					Dictionary<string, AnimationClip> dictionary = animationClips[category];
					if (dictionary.ContainsKey(name))
					{
						return dictionary[name];
					}
					foreach (AnimationClip clip in dictionary.Values)
					{
						if (clip.name.Equals(name, StringComparison.OrdinalIgnoreCase))
						{
							return clip;
						}
					}
					foreach (string key in dictionary.Keys)
					{
						if (key.Contains(name))
						{
							return dictionary[key];
						}
					}
					foreach (AnimationClip clip in dictionary.Values)
					{
						if (clip.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
						{
							return clip;
						}
					}
					LogInfo("No animation named: " + name + " in category: " + category);
				}
				if (animationClips.ContainsKey("general") && category == null)
				{
					Dictionary<string, AnimationClip> dictionary2 = animationClips["general"];
					if (dictionary2.ContainsKey(name))
					{
						return dictionary2[name];
					}
					foreach (AnimationClip clip in dictionary2.Values)
					{
						if (clip.name.Equals(name, StringComparison.OrdinalIgnoreCase))
						{
							return clip;
						}
					}
					foreach (string key2 in dictionary2.Keys)
					{
						if (key2.Contains(name))
						{
							return dictionary2[key2];
						}
					}
					foreach (AnimationClip clip in dictionary2.Values)
					{
						if (clip.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
						{
							return clip;
						}
					}
				}
				if (category == null)
				{
					foreach (Dictionary<string, AnimationClip> clipSource in animationClips.Values)
					{
						if (clipSource.ContainsKey(name))
						{
							return clipSource[name];
						}
						foreach (AnimationClip subClip in clipSource.Values)
						{
							if (subClip.name.Equals(name, StringComparison.OrdinalIgnoreCase))
							{
								return subClip;
							}
						}
						foreach (string clipName in clipSource.Keys)
						{
							if (clipName.Contains(name))
							{
								return clipSource[clipName];
							}
						}
						foreach (AnimationClip clip in clipSource.Values)
						{
							if (clip.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
							{
								return clip;
							}
						}
					}
				}
				LogWarning("No animation found with the name: " + name);
				return null;
			}
		}

		private static Dictionary<string, Animator> raceAnimators = new Dictionary<string, Animator>();

		private static bool raceAnimatorReset = true;

		private static void LoadRaceFBXs()
		{
			string[] raceNames = ["byrdle", "chang", "imp", "Kobold", "poon"];
			string[] raceNamesTemp = raceNames;
			foreach (string name in raceNamesTemp)
			{
				GameObject raceFBX = GameObject.Find(name + "FBX");
				if (raceFBX != null)
				{
					Animator component = raceFBX.GetComponent<Animator>();
					raceAnimators[name] = component;
					LogInfo(name + " loaded into animation memory");
				}
			}
			AnimationLibrary.Instance.PopulateDefaultAnimations();
			raceAnimatorReset = false;
		}
	}

	public class PunkEmotesManager : MonoBehaviour
	{
		private PlayableGraph _playableGraph;

		public Animator _animator;

		private AnimationClipPlayable _currentClipPlayable;

		private AnimationLayerMixerPlayable _layerMixerPlayable;

		private AnimatorOverrideController newOverrideController;

		public Dictionary<string, List<string>> overrideAliases = new Dictionary<string, List<string>>();

		private string _currentAnimation;

		private string _currentCategory;

		private List<string> playerOverrides = new List<string>();

		private Player _player;

		public bool _isAnimationPlaying;

		private void Awake()
		{
			StartCoroutine(WaitForAnimator());
			_animator = GetComponent<Animator>();
			_playableGraph = PlayableGraph.Create();
			_player = GetComponent<Player>();
			_currentClipPlayable = AnimationClipPlayable.Create(_playableGraph, null);
			overrideAliases["sit"] = ["_playeremote_sitinit", "_playeremote_sitloop", "_sitInit", "_sitLoop"];
			overrideAliases["sit2"] = ["_playeremote_sitinit02", "_playeremote_sitloop02", "_sitInit02", "_sitLoop02"];
		}

		private IEnumerator WaitForAnimator()
		{
			_player = GetComponent<Player>();
			if (_player == null)
			{
				LogError("PunkEmotesManager must be attached to a Player object.");
				yield break;
			}
			while (_player._pVisual == null || _player._pVisual._visualAnimator == null)
			{
				yield return null;
			}
			_animator = _player._pVisual._visualAnimator.GetComponent<Animator>();
			if (_animator != null)
			{
				newOverrideController = new AnimatorOverrideController(_animator.runtimeAnimatorController);
				InitializeGraph(_animator);
				LogInfo("Attached PunkEmotesManager to player: " + _player._nickname);
				SendSyncRequest();
			}
			else
			{
				LogError("Animator component not found on PlayerVisual.");
			}
		}

		public void InitializeGraph(Animator animator)
		{
			if (animator == null)
			{
				LogError("Animator is null.");
				return;
			}
			_playableGraph = PlayableGraph.Create("AnimationGraph");
			_layerMixerPlayable = AnimationLayerMixerPlayable.Create(_playableGraph, 2);
			AnimationPlayableOutput val = AnimationPlayableOutput.Create(_playableGraph, "AnimationOutput", animator);
			PlayableOutputExtensions.SetSourcePlayable<AnimationPlayableOutput, AnimationLayerMixerPlayable>(val, _layerMixerPlayable);
		}

		private void SendSyncRequest()
		{
			string text = $"<>#PUNKEMOTES#{_player.netId}#ALL#SYNCREQUEST#";
			GetComponent<ChatBehaviour>().Cmd_SendChatMessage(text, (ChatBehaviour.ChatChannel)3);
		}

		private void SendSyncResponse(string target)
		{
			if (_isAnimationPlaying)
			{
				LogInfo("We're sending the animation to " + target);
				SendAnimationCommand(target, "START", _currentAnimation, this, _currentCategory);
			}
			else
			{
				LogInfo("No animation playing, is this correct?");
			}
			if (playerOverrides != null)
			{
				LogInfo("We're sending override info to " + target);
				{
					foreach (string playerOverride in playerOverrides)
					{
						LogInfo(playerOverride ?? "");
						string[] array = playerOverride.Split('_');
						string animationName = array[0];
						string originOverride = array[1];
						ApplyPunkOverrides(target, this, animationName, originOverride);
					}
					return;
				}
			}
			LogInfo("No override info to send, is this correct?");
		}

		public void ApplyPunkOverrides(string target, PunkEmotesManager emotesManager, string animationName, string originOverride)
		{
			AnimationClip animation = AnimationConstructor.AnimationLibrary.Instance.GetAnimation(animationName, "override");
			if (animation == null)
			{
				return;
			}
			RuntimeAnimatorController runtimeAnimatorController = _animator.runtimeAnimatorController;
			AnimatorOverrideController val = (AnimatorOverrideController)((runtimeAnimatorController is AnimatorOverrideController) ? runtimeAnimatorController : null);
			List<KeyValuePair<AnimationClip, AnimationClip>> list = new List<KeyValuePair<AnimationClip, AnimationClip>>();
			val.GetOverrides(list);
			foreach (KeyValuePair<AnimationClip, AnimationClip> item2 in list)
			{
				newOverrideController[item2.Key] = item2.Value;
			}
			bool flag = false;
			foreach (KeyValuePair<AnimationClip, AnimationClip> item3 in list)
			{
				AnimationClip key = item3.Key;
				if (((key != null) ? (key).name.ToLowerInvariant() : null) == originOverride)
				{
					newOverrideController[item3.Key] = animation;
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				LogError(originOverride + " not found in override mappings.");
				return;
			}
			_animator.runtimeAnimatorController = newOverrideController;
			LogInfo("Applied override: '" + originOverride + "' -> '" + animationName + "'.");
			string item = animationName + "_" + originOverride;
			if (!playerOverrides.Contains(item))
			{
				playerOverrides.Add(item);
			}
			SendAnimationCommand("ALL", "Override", animationName, emotesManager, originOverride);
		}

		public void PlayAnimationClip(string target, PunkEmotesManager emotesManager, string animationName, string animationCategory = null)
		{
			AnimationClip animation = AnimationConstructor.AnimationLibrary.Instance.GetAnimation(animationName, animationCategory);
			if (!emotesManager._playableGraph.IsValid())
			{
				emotesManager.InitializeGraph(emotesManager._animator);
			}
			if (animation == null)
			{
				LogError("AnimationClip is null.");
				return;
			}
			emotesManager.CrossfadeToCustomAnimation(emotesManager, animation);
			AnimationClipPlayable val = AnimationClipPlayable.Create(emotesManager._playableGraph, animation);
			PlayableExtensions.ConnectInput<AnimationLayerMixerPlayable, AnimationClipPlayable>(_layerMixerPlayable, 1, val, 0);
			PlayableExtensions.SetInputWeight<AnimationLayerMixerPlayable>(_layerMixerPlayable, 1, 1f);
			PlayableExtensions.SetInputWeight<AnimationLayerMixerPlayable>(_layerMixerPlayable, 0, 0f);
			emotesManager._playableGraph.Play();
			emotesManager._currentClipPlayable = val;
			emotesManager._isAnimationPlaying = true;
			emotesManager._currentAnimation = animationName;
			emotesManager._currentCategory = animationCategory;
			if (Player._mainPlayer.netId == _player.netId)
			{
				SendAnimationCommand("ALL", "START", animationName, emotesManager, animationCategory);
			}
			LogInfo($"Playing animation clip: {animationName} for player with netId {emotesManager._player.netId}");
		}

		public void StopAnimation(PunkEmotesManager emotesManager)
		{
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			//IL_0052: Unknown result type (might be due to invalid IL or missing references)
			if (emotesManager._isAnimationPlaying && emotesManager._playableGraph.IsValid())
			{
				string animationName = null;
				PlayableExtensions.SetInputWeight<AnimationLayerMixerPlayable>(_layerMixerPlayable, 1, 0f);
				PlayableExtensions.SetInputWeight<AnimationLayerMixerPlayable>(_layerMixerPlayable, 0, 1f);
				emotesManager._playableGraph.Stop();
				PlayableExtensions.Destroy<AnimationClipPlayable>(_currentClipPlayable);
				emotesManager._currentAnimation = null;
				emotesManager._isAnimationPlaying = false;
				SendAnimationCommand("ALL", "STOP", animationName, emotesManager);
				LogInfo($"Stopped custom animation for player with netId {emotesManager._player.netId}.");
			}
		}

		private void CrossfadeToCustomAnimation(PunkEmotesManager emotesManager, AnimationClip animationClip, float crossfadeDuration = 0.3f)
		{
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			RuntimeAnimatorController runtimeAnimatorController = emotesManager._animator.runtimeAnimatorController;
			AnimatorOverrideController val = (AnimatorOverrideController)((runtimeAnimatorController is AnimatorOverrideController) ? runtimeAnimatorController : null);
			if (val != null)
			{
				int num = 0;
				if (emotesManager._animator.layerCount > num)
				{
					AnimatorStateInfo currentAnimatorStateInfo = emotesManager._animator.GetCurrentAnimatorStateInfo(num);
					string text = currentAnimatorStateInfo.IsName("Idle") ? "Idle" : "Default";
					val[text] = animationClip;
					emotesManager._animator.CrossFade(text, crossfadeDuration, num);
				}
				else
				{
					LogError("Base layer not found in animator.");
				}
			}
			LogInfo($"Crossfaded to custom animation: {(animationClip).name} with a {crossfadeDuration}s transition.");
		}

		public void CrossfadeToDefaultState()
		{
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			AnimatorStateInfo currentAnimatorStateInfo = _animator.GetCurrentAnimatorStateInfo(0);
			if (currentAnimatorStateInfo.IsName("Walk") ||
					currentAnimatorStateInfo.IsName("Run") ||
					currentAnimatorStateInfo.IsName("Jump") ||
					currentAnimatorStateInfo.IsName("Dash"))
			{
				_animator.CrossFade(currentAnimatorStateInfo.fullPathHash, 0.2f);
			}
			else
			{
				_animator.CrossFade("Idle", 0.2f);
			}
			LogInfo("Crossfaded back to default state (Idle/Walk/Run) after custom animation.");
		}

		public void Dispose(PunkEmotesManager emotesManager)
		{
			if (emotesManager._playableGraph.IsValid())
			{
				emotesManager._playableGraph.Destroy();
				LogInfo("PlayableGraph destroyed.");
			}
		}

		public void SendAnimationCommand(string target, string command, string animationName, PunkEmotesManager emotesManager, string categoryOrOrigin = null)
		{
			string text = $"<>#PUNKEMOTES#{emotesManager._player.netId}#{target}#{command}#{animationName}#{categoryOrOrigin}";
			_player.GetComponent<ChatBehaviour>().Cmd_SendChatMessage(text, (ChatBehaviour.ChatChannel)3);
		}

		public void HandleChatAnimationMessage(string message)
		{
			if (string.IsNullOrEmpty(message))
			{
				return;
			}
			string[] array = message.ToLower().Split('#', StringSplitOptions.None);
			if (array.Length >= 5)
			{
				if (uint.TryParse(array[2], out var result))
				{
					string target = array[3];
					string request = array[4];
					string aniName = array[5];
					string? aniCat = array.Length > 6 ? array[6] : null;
					if (result == Player._mainPlayer.netId)
					{
						LogInfo("Skipping local player animation reprocessing to prevent infinite loop.");
					}
					else
					{
						if (!(target == "all") && (!uint.TryParse(target, out var result2) || result2 != Player._mainPlayer.netId))
						{
							return;
						}
						Player playerByNetId = PlayerRegistry.GetPlayerByNetId(result);
						if (playerByNetId != null)
						{
							PunkEmotesManager component = playerByNetId.GetComponent<PunkEmotesManager>();
							if (component != null)
							{
								switch (request)
								{
									case "syncrequest":
										LogInfo("Sync requested, sending response:");
										SendSyncResponse(result.ToString());
										break;
									case "start":
										if (!string.IsNullOrEmpty(aniCat))
										{
											component.PlayAnimationClip(null, component, aniName, aniCat);
										}
										else
										{
											component.PlayAnimationClip(null, component, aniName);
										}
										break;
									case "override":
										if (!string.IsNullOrEmpty(aniCat))
										{
											if (overrideAliases.ContainsKey(aniCat))
											{
												List<string> list = overrideAliases[aniCat];
												string animationName = aniName + list[2];
												string animationName2 = aniName + list[3];
												component.ApplyPunkOverrides(null, component, animationName, list[0]);
												component.ApplyPunkOverrides(null, component, animationName2, list[1]);
											}
											else
											{
												component.ApplyPunkOverrides(null, component, aniName, aniCat);
											}
										}
										else
										{
											LogWarning("Override command missing originOverride for animation '" + aniName + "'.");
										}
										break;
									case "stop":
										component.StopAnimation(component);
										break;
									default:
										LogWarning("Unknown command '" + request + "' received in PUNKEMOTES message.");
										break;
								}
							}
							else
							{
								LogWarning($"PunkEmotesManager not found for sender player with netId '{result}'.");
							}
						}
						else
						{
							LogWarning($"Sender player with netId '{result}' not found.");
						}
					}
				}
				else
				{
					LogWarning("Failed to parse sender's netId from message: " + array[2]);
				}
			}
			else
			{
				LogWarning("Invalid PUNKEMOTES message format. Insufficient parts.");
			}
		}
	}

	public static class PlayerRegistry
	{
		private class PlayerEntry
		{
			public string Nickname { get; set; }

			public Player PlayerInstance { get; set; }

			public PunkEmotesManager EmotesManager { get; set; }
		}

		private static Dictionary<uint, PlayerEntry> _playersByNetId = new Dictionary<uint, PlayerEntry>();

		public static void RegisterPlayer(Player player, PunkEmotesManager emotesManager)
		{
			if (player != null && emotesManager != null)
			{
				uint netId = player.netId;
				if (!_playersByNetId.ContainsKey(netId))
				{
					_playersByNetId[netId] = new PlayerEntry
					{
						Nickname = player.Network_nickname,
						PlayerInstance = player,
						EmotesManager = emotesManager
					};
				}
			}
		}

		public static void UnregisterPlayer(Player player)
		{
			if (player != null)
			{
				uint netId = player.netId;
				_playersByNetId.Remove(netId);
			}
		}

		public static void ClearRegistry()
		{
			_playersByNetId.Clear();
		}

		public static PunkEmotesManager GetEmotesManagerByNetId(uint netId)
		{
			PlayerEntry value;
			return _playersByNetId.TryGetValue(netId, out value) ? value.EmotesManager : null;
		}

		public static Player GetPlayerByNetId(uint netId)
		{
			PlayerEntry value;
			return _playersByNetId.TryGetValue(netId, out value) ? value.PlayerInstance : null;
		}

		public static Player GetPlayerByNickname(string nickname)
		{
			foreach (PlayerEntry value in _playersByNetId.Values)
			{
				if (value.Nickname == nickname)
				{
					return value.PlayerInstance;
				}
			}
			return null;
		}
	}

	[HarmonyPatch]
	public static class PatchWrapper
	{
		[HarmonyPatch(typeof(ChatBehaviour), "Send_ChatMessage")]
		public class PlayPunkEmote
		{
			private static bool Prefix(ref string _message, ChatBehaviour __instance)
			{
				if (string.IsNullOrEmpty(_message))
				{
					return true;
				}
				if (!_message.StartsWith("/em ", StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
				PunkEmotesManager emotesManagerByNetId = PlayerRegistry.GetEmotesManagerByNetId(Player._mainPlayer.netId);
				string text = _message.Substring(4).Trim();
				string[] array = text.Split(' ');
				string text2 = array[0].ToLower();
				string text3 = text2;
				if (!(text3 == "overrides"))
				{
					if (text3 == "help")
					{
						SendChatMessage("Commands: '/em animation_name (or race)'");
						SendChatMessage("Commands: '/em category animation_name (or race)'");
						SendChatMessage("Categories: 'sit', 'dance'");
						SendChatMessage("Test animation: '/em 02'");
						LogInfo("Available commands: overrides, help");
						return false;
					}
					if (array.Length == 3 && array[0].ToLower() == "override")
					{
						string text4 = array[1].ToLower();
						string text5 = array[2].ToLower();
						if (AnimationConstructor.AnimationLibrary.Instance.GetAnimation(text5, "override") == null)
						{
							LogError("Override animation '" + text5 + "' not found.");
							return false;
						}
						if (emotesManagerByNetId.overrideAliases.ContainsKey(text4))
						{
							List<string> list = emotesManagerByNetId.overrideAliases[text4];
							string animationName = text5 + list[2];
							string animationName2 = text5 + list[3];
							emotesManagerByNetId.ApplyPunkOverrides("ALL", emotesManagerByNetId, animationName, list[0]);
							emotesManagerByNetId.ApplyPunkOverrides("ALL", emotesManagerByNetId, animationName2, list[1]);
						}
						else
						{
							emotesManagerByNetId.ApplyPunkOverrides("ALL", emotesManagerByNetId, text5, text4);
						}
						return false;
					}
					if (array.Length == 2)
					{
						string animationCategory = array[0].ToLower();
						string animationName3 = array[1].ToLower();
						emotesManagerByNetId.PlayAnimationClip("ALL", emotesManagerByNetId, animationName3, animationCategory);
						return false;
					}
					if (array.Length == 1)
					{
						string animationName4 = array[0].ToLower();
						emotesManagerByNetId.PlayAnimationClip("ALL", emotesManagerByNetId, animationName4);
						return false;
					}
					LogWarning("Invalid emotes format. Expected '/em [category] [name]', '/em [name]', or '/em override [originOverride] [newOverride]'.");
					return false;
				}
				AnimationClip[] animationClips = emotesManagerByNetId._animator.runtimeAnimatorController.animationClips;
				if (animationClips != null && animationClips.Length != 0)
				{
					AnimationClip[] array2 = animationClips;
					foreach (AnimationClip val in array2)
					{
						LogInfo("Overridable animation: " + (val).name);
					}
				}
				else
				{
					LogWarning("No animation clips found in the Animator.");
				}
				return false;
			}
		}

		[HarmonyPatch(typeof(PlayerMove), "Set_MovementAction")]
		public class SetMovementActionPatch
		{
			private static void Postfix(PlayerMove __instance, MovementAction _mA)
			{
				//IL_0030: Unknown result type (might be due to invalid IL or missing references)
				Player component = __instance.gameObject.GetComponent<Player>();
				if (!(component == null))
				{
					PunkEmotesManager component2 = component.GetComponent<PunkEmotesManager>();
					if (!(component2 == null) && (int)_mA != 0 && component2._isAnimationPlaying)
					{
						component2.StopAnimation(component2);
					}
				}
			}
		}

		private static MethodInfo rpcMethod = typeof(ChatBehaviour).GetMethod("Rpc_RecieveChatMessage", BindingFlags.Instance | BindingFlags.NonPublic);

		[HarmonyPatch(typeof(ChatBehaviour), "UserCode_Rpc_RecieveChatMessage__String__Boolean__ChatChannel")]
		[HarmonyPrefix]
		public static bool UserCode_Rpc_RecieveChatMessage__String__Boolean__ChatChannel_Prefix(string message, bool _isEmoteMessage, ChatBehaviour.ChatChannel _chatChannel)
		{
			LogInfo(message);
			if (message.Contains("<>#PUNKEMOTES#"))
			{
				LogInfo("PUNKEMOTES detected in RPC!");
				string[] array = message.Split(new string[1] { "#" }, StringSplitOptions.None);
				if (array.Length >= 4)
				{
					if (!uint.TryParse(array[2], out var result))
					{
						LogWarning("Failed to parse netId from message: " + array[2]);
						return false;
					}
					Player playerByNetId = PlayerRegistry.GetPlayerByNetId(result);
					if (!(playerByNetId != null))
					{
						LogWarning($"Player with netId '{result}' not found.");
						return false;
					}
					PunkEmotesManager component = playerByNetId.GetComponent<PunkEmotesManager>();
					if (component != null)
					{
						component.HandleChatAnimationMessage(message);
						return false;
					}
				}
				return false;
			}
			return true;
		}

		[HarmonyPatch(typeof(ChatBehaviour), nameof(ChatBehaviour.UserCode_Cmd_SendChatMessage__String__ChatChannel))]
		[HarmonyPrefix]
		public static bool UserCode_Cmd_SendChatMessage_Prefix(ChatBehaviour __instance, string _message, ChatBehaviour.ChatChannel _chatChannel)
		{
			if (_message.Contains("<>#PUNKEMOTES#"))
			{
				if (rpcMethod != null)
				{
					rpcMethod.Invoke(__instance, new object[3]
					{
						_message,
						true,
						(ChatBehaviour.ChatChannel)3
					});
					LogInfo("Caught <>#PUNKEMOTES#, sent to RPC");
					return false;
				}
				return false;
			}
			return true;
		}

		[HarmonyPatch(typeof(Player), "Start")]
		private static void Postfix(Player __instance)
		{
			PunkEmotesManager punkEmotesManager = __instance.gameObject.GetComponent<PunkEmotesManager>();
			if (punkEmotesManager == null)
			{
				punkEmotesManager = __instance.gameObject.AddComponent<PunkEmotesManager>();
			}
			PlayerRegistry.RegisterPlayer(__instance, punkEmotesManager);
		}
	}

	internal static ManualLogSource Logger;

	public static bool logInfoEnabled = false;

	public static bool logWarningEnabled = true;

	public static bool logErrorEnabled = true;

	private static bool logMethod = true;

	private void Awake()
	{
		Logger = base.Logger;
		LogInfo("Punk Emotes is rockin'!");
		Harmony val = new("punkalyn.punkemotes");
		int patchCount = 7;
		try
		{
			val.PatchAll();
			if (patchCount != val.GetPatchedMethods().Count())
			{
				LogError($"Punk Emotes patched {val.GetPatchedMethods().Count()} methods out of {patchCount} intended patches!");
			}
		}
		catch (Exception ex)
		{
			LogError("Exception caught while patching: " + ex.Message);
		}
	}

	public static void LogMethod(bool enable)
	{
		logMethod = enable;
	}

	public static void LogInfo(string message, bool? shouldLog = null)
	{
		if ((shouldLog ?? logInfoEnabled) && logMethod)
		{
			Logger.LogInfo(message);
		}
	}

	public static void LogWarning(string message, bool? shouldLog = null)
	{
		if ((shouldLog ?? logWarningEnabled) && logMethod)
		{
			Logger.LogWarning(message);
		}
	}

	public static void LogError(string message, bool? shouldLog = null)
	{
		if ((shouldLog ?? logErrorEnabled) && logMethod)
		{
			Logger.LogError(message);
		}
	}

	private static void SendChatMessage(string message)
	{
		if (Player._mainPlayer)
			Player._mainPlayer._cB.New_ChatMessage(message);
	}
}
