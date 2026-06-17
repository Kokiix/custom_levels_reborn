using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using ComputerysModdingUtilities;
using CustomLevelsReborn;
using HarmonyLib;
using HarmonyLib.Tools;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[assembly: StraftatMod(isVanillaCompatible: false)]

[BepInDependency(MyceliumNetworking.MyPluginInfo.PLUGIN_GUID)]
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class CLRPlugin : BaseUnityPlugin
{
    internal static ManualLogSource Log;
    internal static List<string> MapVersions = [];

    internal static Sprite MapDisabledSprite;

    // These could be unified but I'm lazy
    internal static Dictionary<string, string> SceneToBundleDir = [];
    internal static Dictionary<string, Texture2D> MapThumbnails = [];
    internal static Dictionary<string, GameObject> MapDisabledSprites = [];

    Harmony _harmony = new(MyPluginInfo.PLUGIN_GUID);

    static string PluginDir;

    void Awake()
    {
        Log = Logger;
        this.gameObject.hideFlags = HideFlags.HideAndDontSave;
        _harmony.PatchAll();

        PluginDir = Path.GetDirectoryName(Info.Location);
        if (PluginDir == null)
            PluginDir = Path.Combine(Paths.PluginPath, "DEVELOPMENT-BUILD-Custom Levels Reborn");
        FindBundles();

        gameObject.AddComponent<SyncMaps>();

        SceneManager.sceneLoaded += StealSceneGOs.OnSceneLoad;
    }

    // _resources bundles and clr_shared bundle are currently always loaded.
    void FindBundles()
    {
        var shared = AssetBundle.LoadFromFile(Path.Combine(PluginDir, "clr_shared"));
        MapDisabledSprite = shared.LoadAsset<Sprite>("MapDisableOverlay");

        foreach (var modDir in Directory.EnumerateDirectories(Paths.PluginPath))
        {
            var packageVer = GetPackageVersion(modDir);
            var customMapsDir = Directory.GetDirectories(modDir, "CustomMaps", SearchOption.AllDirectories).FirstOrDefault();
            if (!customMapsDir.IsNullOrWhiteSpace())
                LoadBundles(customMapsDir, packageVer);
        }

        Logger.LogInfo("Loaded maps: " + string.Join(", ", MapVersions));
    }

    HashSet<string> seenBundles = [];
    void LoadBundles(string dir, string packageVer)
    {
        foreach (var filePath in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
        {
            var bundleName = Path.GetFileName(filePath);
            if (seenBundles.Contains(bundleName)) continue;
            else seenBundles.Add(bundleName);

            var bundle = AssetBundle.LoadFromFile(filePath);
            if (bundle.isStreamedSceneAssetBundle)
            {
                // Technically, I could get the scene data from some metadata file to avoid this load and unload,
                // but that would require some more work by the user, then reconciling things if that doesn't 
                // match any actual scene path... At least this loads them one at a time?
                foreach (var scene in bundle.GetAllScenePaths()
                .Select(Path.GetFileNameWithoutExtension))
                {
                    if (!SceneToBundleDir.ContainsKey(scene))
                    {
                        SceneToBundleDir.Add(scene, filePath);
                        MapVersions.Add(scene + "-v" + packageVer);
                    }
                }

                bundle.Unload(true);
            }
            else if (filePath.EndsWith("_resources"))
            {
                foreach (var tnail in bundle.LoadAllAssets<Texture2D>())
                {
                    MapThumbnails.Add(tnail.name, tnail);
                }
            }
        }
    }


    [Serializable]
    public class ThunderstoreManifest
    {
        public string name;
        public string version_number;
        public string website_url;
        public string description;
    }

    string GetPackageVersion(string folder)
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