using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Packages.Rider.Editor.ProjectGeneration
{
  [Serializable]
  internal class RiderProjectConfig
  {
    public List<PlatformProjectConfig> platforms = new List<PlatformProjectConfig>();
    public List<CustomProjectConfig> customProjects = new List<CustomProjectConfig>();
    public bool groupProjectsByName = true;
    public int groupProjectsByNameDepth = 2;
  }

  [Serializable]
  internal class PlatformProjectConfig
  {
    public string name; // e.g., "Android", "iOS"
    public bool enabled;
    public string defines; // Semicolon separated

    public PlatformProjectConfig(string name, string defaultDefines)
    {
      this.name = name;
      this.enabled = false;
      this.defines = defaultDefines;
    }
  }

  [Serializable]
  internal class CustomProjectConfig
  {
    public string name;
    public string defines;
    public bool enabled = true;
  }

  internal static class RiderProjectConfigStorage
  {
    private static string ConfigPath => System.IO.Path.Combine(Application.dataPath, "..", "ProjectSettings", "RiderProjectGenerationConfig.json");

    public static RiderProjectConfig Load()
    {
      if (!System.IO.File.Exists(ConfigPath))
      {
        return CreateDefault();
      }
      try
      {
        var json = System.IO.File.ReadAllText(ConfigPath);
        return JsonUtility.FromJson<RiderProjectConfig>(json) ?? CreateDefault();
      }
      catch (Exception e)
      {
        Debug.LogWarning($"Failed to load Rider project generation config: {e.Message}");
        return CreateDefault();
      }
    }

    public static void Save(RiderProjectConfig config)
    {
      try
      {
        var json = JsonUtility.ToJson(config, true);
        System.IO.File.WriteAllText(ConfigPath, json);
      }
      catch (Exception e)
      {
        Debug.LogError($"Failed to save Rider project generation config: {e.Message}");
      }
    }

    private static RiderProjectConfig CreateDefault()
    {
      var config = new RiderProjectConfig();
      // Windows,Linux,MacOS,Android,iOS,Playstation 4, Playstation 5, Xbox One,Xbox Series, Switch 1, Switch 2
      config.platforms.Add(new PlatformProjectConfig("Windows", "UNITY_STANDALONE;UNITY_STANDALONE_WIN"));
      config.platforms.Add(new PlatformProjectConfig("Linux", "UNITY_STANDALONE;UNITY_STANDALONE_LINUX"));
      config.platforms.Add(new PlatformProjectConfig("MacOS", "UNITY_STANDALONE;UNITY_STANDALONE_OSX"));
      config.platforms.Add(new PlatformProjectConfig("Android", "UNITY_ANDROID"));
      config.platforms.Add(new PlatformProjectConfig("iOS", "UNITY_IOS"));
      config.platforms.Add(new PlatformProjectConfig("PS4", "UNITY_PS4"));
      config.platforms.Add(new PlatformProjectConfig("PS5", "UNITY_PS5"));
      config.platforms.Add(new PlatformProjectConfig("XboxOne", "UNITY_XBOXONE"));
      config.platforms.Add(new PlatformProjectConfig("XboxSeries", "UNITY_GAMECORE;UNITY_GAMECORE_XBOXSERIES"));
      config.platforms.Add(new PlatformProjectConfig("Switch", "UNITY_SWITCH"));
      config.platforms.Add(new PlatformProjectConfig("Switch 2", "UNITY_SWITCH_2")); // Placeholder define, user can edit

      return config;
    }
  }
}
