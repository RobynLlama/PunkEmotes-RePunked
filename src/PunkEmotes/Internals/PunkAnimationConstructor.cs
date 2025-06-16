using System;
using System.Collections.Generic;
using UnityEngine;

namespace PunkEmotes.Internals;

public class AnimationConstructor
{
  public static Dictionary<string, Animator> raceAnimators = [];
  internal static bool raceAnimatorReset = true;

  // Method to load all race-specific FBXs (you can call this once at the start or when a race is loaded)
  public static void LoadRaceFBXs()
  {
    // Assuming you know the list of race names
    string[] raceNames = ["byrdle", "chang", "imp", "Kobold", "poon"];

    foreach (var race in raceNames)
    {
      GameObject raceFBX = GameObject.Find(race + "FBX");
      if (raceFBX != null)
      {
        Animator raceAnimator = raceFBX.GetComponent<Animator>();
        raceAnimators[race] = raceAnimator;
        PunkEmotesPlugin.Log.LogInfo($"{race} loaded into animation memory");
      }
    }
    PunkEmotesLibrary.Instance.PopulateDefaultAnimations();
    raceAnimatorReset = false;
  }

  public class PunkEmotesLibrary
  {
    // Static instance for the Singleton pattern
    private static PunkEmotesLibrary _instance;

    // Dictionary to store animations by their name
    private Dictionary<string, Dictionary<string, AnimationClip>> animationClips =
        new Dictionary<string, Dictionary<string, AnimationClip>>()
        {
                    { "general", new Dictionary<string, AnimationClip>() }, // Uncategorized clips fall in here as a fallback case
                    { "atlyss", new Dictionary<string, AnimationClip>() }, // Atlyss animations are added here so that they're easier to find
                    { "override_playerEmote_sitInit", new Dictionary<string, AnimationClip>() },
                    { "override_playerEmote_sitLoop", new Dictionary<string, AnimationClip>() },
                    { "override_playerEmote_sitInit02", new Dictionary<string, AnimationClip>() },
                    { "override_playerEmote_sitLoop02", new Dictionary<string, AnimationClip>() },
                    { "overrideplayer_idle", new Dictionary<string, AnimationClip>() },
                    { "dance", new Dictionary<string, AnimationClip>() },
                    { "sit", new Dictionary<string, AnimationClip>() },
        };

    // Private constructor to prevent instantiation from outside
    private PunkEmotesLibrary() { }

    // Public static property to access the singleton instance
    public static PunkEmotesLibrary Instance
    {
      get
      {
        if (_instance == null)
        {
          _instance = new PunkEmotesLibrary();
        }
        return _instance;
      }
    }

    public void PopulateDefaultAnimations()
    {
      // Loop over each race in the raceAnimators dictionary
      foreach (KeyValuePair<string, Animator> raceAnimatorPair in raceAnimators)
      {
        string raceName = raceAnimatorPair.Key;
        Animator raceAnimator = raceAnimatorPair.Value;

        // Extract animation clips based on specific keywords
        AnimationClip[] clips = ExtractAnimationsFromAnimator(raceAnimator);

        // Loop through each clip and check if it contains the desired animation names
        foreach (AnimationClip clip in clips)
        {
          if (clip != null)
          {
            if (clip.name.EndsWith("dance"))
            {
              if (clip.name == "Kobold_dance")
              {
                clip.name = "kubold_dance";
                raceName = "kubold";
              }
              // Add dance animation to the library
              animationClips["atlyss"][$"{raceName}_dance"] = clip;
              animationClips["dance"][$"{raceName}_dance"] = clip;
              PunkEmotesPlugin.Log.LogInfo($"Added {clip.name} as {raceName}_dance to animation library!");
            }

            if (clip.name.EndsWith("sitInit"))
            {
              if (clip.name == "Kobold_sitInit")
              {
                clip.name = "kubold_sitinit";
                raceName = "kubold";
              }

              animationClips["atlyss"][$"{raceName}_sitinit"] = clip;
              animationClips["override_playerEmote_sitInit"][$"{raceName}_sitinit"] = clip;
              animationClips["sit"][$"{raceName}_sitinit"] = clip;
              PunkEmotesPlugin.Log.LogInfo($"Added {clip.name} as {raceName}_sitinit to animation library!");
            }



            if (clip.name.EndsWith("sitLoop"))
            {
              if (clip.name == "Kobold_sitLoop")
              {
                clip.name = "kubold_sitloop";
                raceName = "kubold";
              }

              animationClips["atlyss"][$"{raceName}_sitloop"] = clip;
              animationClips["override_playerEmote_sitLoop"][$"{raceName}_sitloop"] = clip;
              animationClips["sit"][$"{raceName}_sitloop"] = clip;
              PunkEmotesPlugin.Log.LogInfo($"Added {clip.name} as {raceName}_sitloop to animation library!");
            }



            if (clip.name.EndsWith("sitInit02"))
            {
              if (clip.name == "Kobold_sitInit02")
              {
                clip.name = "kubold_sitinit2";
                raceName = "kubold";
              }

              animationClips["atlyss"][$"{raceName}_sitinit02"] = clip;
              animationClips["override_playerEmote_sitInit02"][$"{raceName}_sitinit02"] = clip;
              animationClips["sit"][$"{raceName}_sitinit02"] = clip;
              PunkEmotesPlugin.Log.LogInfo($"Added {clip.name} as {raceName}_sitinit02 to animation library!");
            }



            if (clip.name.EndsWith("sitLoop02"))
            {
              if (clip.name == "Kobold_sitLoop")
              {
                clip.name = "kubold_sitloop02";
              }

              animationClips["atlyss"][$"{raceName}_sitloop02"] = clip;
              animationClips["override_playerEmote_sitLoop02"][$"{raceName}_sitloop02"] = clip;
              animationClips["sit"][$"{raceName}_sitloop02"] = clip;
              PunkEmotesPlugin.Log.LogInfo($"Added {clip.name} as {raceName}_sitloop02 to animation library!");
            }
          }
        }
      }
    }

    // Helper method to extract animations from an Animator
    private AnimationClip[] ExtractAnimationsFromAnimator(Animator animator)
    {
      // Retrieve all animation clips from the Animator
      List<AnimationClip> clips = new List<AnimationClip>();

      foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
      {
        clips.Add(clip);
      }

      return clips.ToArray();
    }

    // Method to initialize and load animations (can be done at runtime)
    public void LoadAnimations()
    {
      //Load animations from folder, if they exist
      AnimationClip[] clips = Resources.LoadAll<AnimationClip>("Animations/");

      foreach (var clip in clips)
      {
        // Add to main dictionary
        // animationClips[clip.name] = clip;
      }
    }

    public string NormalizeAnimationName(string animationName, string category = null)
    {
      if (!string.IsNullOrEmpty(category))
      {
        PunkEmotesPlugin.Log.LogInfo($"Searching for animation {animationName} in {category}");
        // This could be the resolved name or null if it can't be found
        var clip = GetAnimation(animationName, category);
        if (clip != null)
        {
          PunkEmotesPlugin.Log.LogInfo($"Found animation for {animationName}: {clip.name} in {category}.");
        }
        return clip != null ? clip.name : null;
      }
      else
      {
        var clip = GetAnimation(animationName);
        PunkEmotesPlugin.Log.LogInfo($"Found animation for {animationName}: {clip.name} without a category.");
        return clip == null ? null : clip.name;
      }
    }

    // Get an animation by its name
    public AnimationClip GetAnimation(string name, string category = null)
    {
      // Normalize input for case-insensitive matching
      name = name.ToLowerInvariant();

      // Check if the category exists
      if (!string.IsNullOrEmpty(category) && animationClips.ContainsKey(category))
      {
        var animationsInCategory = animationClips[category];

        // Exact match by key
        if (animationsInCategory.ContainsKey(name))
        {
          PunkEmotesPlugin.Log.LogInfo($"Found animation by exact key match: {animationsInCategory[name]} in: {category}");
          return animationsInCategory[name];
        }

        // Exact match by clip name (value)
        foreach (var clip in animationsInCategory.Values)
        {
          if (clip.name.Equals(name, StringComparison.OrdinalIgnoreCase))
          {
            PunkEmotesPlugin.Log.LogInfo($"Found animation by exact clip name match: {clip.name} in: {category}");
            return clip;
          }
        }

        // Fallback: Partial match by key
        foreach (var key in animationsInCategory.Keys)
        {
          if (key.Contains(name))
          {
            PunkEmotesPlugin.Log.LogInfo($"Found animation by partial key match: {animationsInCategory[key]} in: {category}");
            return animationsInCategory[key];
          }
        }

        // Fallback: Partial match by clip name (value)
        foreach (var clip in animationsInCategory.Values)
        {
          if (clip.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
          {
            PunkEmotesPlugin.Log.LogInfo($"Found animation by partial clip name match: {clip.name} in: {category}");
            return clip;
          }
        }
        PunkEmotesPlugin.Log.LogInfo($"No animation named: {name} in category: {category}");
      }

      // Check the "general" category before searching all categories
      if (animationClips.ContainsKey("general") && category == null)
      {
        var generalAnimations = animationClips["general"];

        // Exact match by key in "general"
        if (generalAnimations.ContainsKey(name))
        {
          PunkEmotesPlugin.Log.LogInfo($"Found animation by exact key match: {generalAnimations[name]} in: General");
          return generalAnimations[name];
        }

        // Exact match by clip name in "general"
        foreach (var clip in generalAnimations.Values)
        {
          if (clip.name.Equals(name, StringComparison.OrdinalIgnoreCase))
          {
            PunkEmotesPlugin.Log.LogInfo($"Found animation by exact clip name match: {clip.name} in: General");
            return clip;
          }
        }

        // Fallback: Partial match by key in "general"
        foreach (var key in generalAnimations.Keys)
        {
          if (key.Contains(name))
          {
            PunkEmotesPlugin.Log.LogInfo($"Found animation by partial key match: {generalAnimations[key]} in: General");
            return generalAnimations[key];
          }
        }

        // Fallback: Partial match by clip name in "general"
        foreach (var clip in generalAnimations.Values)
        {
          if (clip.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
          {
            PunkEmotesPlugin.Log.LogInfo($"Found animation by partial clip name match: {clip.name} in: General");
            return clip;
          }
        }
        PunkEmotesPlugin.Log.LogInfo($"No animation found with the name: {name} in category: General");
      }

      if (category == null)
      {
        // Global fallback: Search all categories
        foreach (var allAnimations in animationClips.Values)
        {
          // Exact match by key
          if (allAnimations.ContainsKey(name))
          {
            PunkEmotesPlugin.Log.LogInfo($"Found animation by exact key match: {allAnimations[name]} in: AnimationLibrary");
            return allAnimations[name];
          }

          // Exact match by clip name
          foreach (var clip in allAnimations.Values)
          {
            if (clip.name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
              PunkEmotesPlugin.Log.LogInfo($"Found animation by exact clip name match: {clip.name} in: AnimationLibrary");
              return clip;
            }
          }

          // Fallback: Partial match by key
          foreach (var key in allAnimations.Keys)
          {
            if (key.Contains(name))
            {
              PunkEmotesPlugin.Log.LogInfo($"Found animation by partial key match: {allAnimations[key]} in: AnimationLibrary");
              return allAnimations[key];
            }
          }

          // Fallback: Partial match by clip name
          foreach (var clip in allAnimations.Values)
          {
            if (clip.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
            {
              PunkEmotesPlugin.Log.LogInfo($"Found animation by partial clip name match: {clip.name} in: AnimationLibrary");
              return clip;
            }
          }
        }
      }
      // Fail state: No match found
      PunkEmotesPlugin.Log.LogWarning($"No animation found with the name: {name}");
      return null;
    }
  }
}
