using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using ComputerysModdingUtilities;
using CustomLevelsReborn;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

[assembly: StraftatMod(isVanillaCompatible: false)]

[BepInDependency(MyceliumNetworking.MyPluginInfo.PLUGIN_GUID)]
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class CLRPlugin : BaseUnityPlugin
{
    internal static ManualLogSource Log;
    internal static List<string> MapVersions = [];
    internal static Dictionary<string, string> SceneToBundleDir = [];
    internal static Dictionary<string, Texture2D> MapThumbnails = [];

    Harmony _harmony = new(MyPluginInfo.PLUGIN_GUID);

    void Awake()
    {
        Log = Logger;
        this.gameObject.hideFlags = HideFlags.HideAndDontSave;
        _harmony.PatchAll();

        LoadBundles();

        gameObject.AddComponent<SyncMaps>();

        SceneManager.sceneLoaded += StealSceneGOs.OnSceneLoad;
    }

    void OnDestroy()
    {
        _harmony.UnpatchSelf();
    }

    void LoadBundles()
    {
        string myPluginDir = Path.GetDirectoryName(Info.Location);
        AssetBundle.LoadFromFile(Path.Combine(myPluginDir, "shared")); // Potentially move to dynBundleLoad, tho the file is currently microscopic in size

        foreach (var pluginDir in Directory.EnumerateDirectories(Paths.PluginPath))
        {
            foreach (var folder in Directory.EnumerateDirectories(pluginDir))
            {
                if (folder.EndsWith("CustomMaps"))
                {
                    var pluginVersion = GetPluginVersion(pluginDir);

                    foreach (var filePath in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
                    {
                        var bundle = AssetBundle.LoadFromFile(filePath);
                        if (bundle.isStreamedSceneAssetBundle)
                        {
                            // Technically, I could get the scene data from some metadata file to avoid this load and unload,
                            // but that would require some more work by the user, then reconciling things if that doesn't 
                            // match any actual scene path... At least this loads them one at a time?
                            foreach (var scene in bundle.GetAllScenePaths()
                            .Select(Path.GetFileNameWithoutExtension))
                            {
                                SceneToBundleDir.Add(scene, filePath);
                                MapVersions.Add(scene + "v" + pluginVersion);
                            }

                            bundle.UnloadAsync(false);
                        }
                        else if (filePath.EndsWith("_resources"))
                        {
                            foreach (var tnail in bundle.LoadAllAssets<Texture2D>())
                            {
                                MapThumbnails.Add(tnail.name, tnail);
                            }
                        }
                    }

                    break;
                }
            }
        }

        Logger.LogInfo("Loaded maps: " + string.Join(", ", MapVersions));
    }

    [Serializable]
    public class ThunderstoreManifest
    {
        public string name;
        public string version_number;
        public string website_url;
        public string description;
    }

    string GetPluginVersion(string folder)
    {
        string manifestPath = Path.Combine(folder, "manifest.json");
        try
        {
            string jsonText = File.ReadAllText(manifestPath);
            var manifest = JsonUtility.FromJson<ThunderstoreManifest>(jsonText);

            if (manifest != null && !string.IsNullOrEmpty(manifest.version_number))
            {
                return manifest.version_number;
            }
        }
        catch (Exception) { }
        return "{NO_VERSION_FOUND}";
    }
}