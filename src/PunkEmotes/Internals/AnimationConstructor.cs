using System;
using System.Collections.Generic;
using UnityEngine;

namespace PunkEmotes.Internals;

public class AnimationConstructor
{
  public class AnimationLibrary
  {
    private static AnimationLibrary? _instance;

    internal readonly Dictionary<string, Dictionary<string, AnimationClip>> animationClips = new(StringComparer.InvariantCultureIgnoreCase)
      {
        {
          "dance",
          new Dictionary<string, AnimationClip>(StringComparer.InvariantCultureIgnoreCase)
        },
        {
          "general",
          new Dictionary<string, AnimationClip>(StringComparer.InvariantCultureIgnoreCase)
        },
        {
          "override",
          new Dictionary<string, AnimationClip>(StringComparer.InvariantCultureIgnoreCase)
        },
        {
          "sit",
          new Dictionary<string, AnimationClip>(StringComparer.InvariantCultureIgnoreCase)
        }
      };

    public static AnimationLibrary Instance
    {
      get
      {
        _instance ??= new AnimationLibrary();
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
            PunkEmotesPlugin.Log.LogDebug("Added " + clip.name + " as " + key + "_dance to animation library!");
          }
          if (clip.name.Contains("sitInit") && !clip.name.Contains("02"))
          {
            if (clip.name == "Kobold_sitInit")
            {
              clip.name = "kubold_sitInit";
            }
            animationClips["override"][key + "_sitInit"] = clip;
            animationClips["sit"][key + "_sitInit"] = clip;
            PunkEmotesPlugin.Log.LogDebug("Added " + clip.name + " as " + key + "_sitInit to animation library!");
          }
          if (clip.name.Contains("sitLoop") && !clip.name.Contains("02"))
          {
            if (clip.name == "Kobold_sitLoop")
            {
              clip.name = "kubold_sitLoop";
            }
            animationClips["override"][key + "_sitLoop"] = clip;
            PunkEmotesPlugin.Log.LogDebug("Added " + clip.name + " as " + key + "_sitLoop to animation library!");
          }
          if (clip.name.Contains("sitInit02"))
          {
            if (clip.name == "Kobold_sitInit02")
            {
              clip.name = "kubold_sitInit02";
            }
            animationClips["override"][key + "_sitInit02"] = clip;
            PunkEmotesPlugin.Log.LogDebug("Added " + clip.name + " as " + key + "_sitInit02 to animation library!");
          }
          if (clip.name.Contains("sitLoop02"))
          {
            if (clip.name == "Kobold_sitLoop")
            {
              clip.name = "kubold_sitLoop";
            }
            animationClips["override"][key + "_sitLoop02"] = clip;
            PunkEmotesPlugin.Log.LogDebug("Added " + clip.name + " as " + key + "_sitLoop02 to animation library!");
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

    public AnimationClip? GetAnimation(string name, string? category)
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
        PunkEmotesPlugin.Log.LogDebug("No animation named: " + name + " in category: " + category);
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
      PunkEmotesPlugin.Log.LogWarning("No animation found with the name: " + name);
      return null;
    }
  }

  private static Dictionary<string, Animator> raceAnimators = new Dictionary<string, Animator>();

  internal static bool raceAnimatorReset = true;

  internal static void LoadRaceFBXs()
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
        PunkEmotesPlugin.Log.LogDebug(name + " loaded into animation memory");
      }
    }
    AnimationLibrary.Instance.PopulateDefaultAnimations();
    raceAnimatorReset = false;
  }
}
