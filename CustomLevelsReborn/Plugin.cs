using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using ComputerysModdingUtilities;
using CustomLevelsReborn;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

[assembly: StraftatMod(isVanillaCompatible: false)]

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class CLRPlugin : BaseUnityPlugin
{
    internal static List<string> ScenePaths = [];
    // internal static Dictionary<string, AssetBundle> ResourceBundles = [];
    internal static Dictionary<string, Texture2D> MapThumbnails = [];

    Harmony _harmony = new(MyPluginInfo.PLUGIN_GUID);

    void Awake()
    {
        this.gameObject.hideFlags = HideFlags.HideAndDontSave;
        _harmony.PatchAll();

        RefreshBundles();
        StealSceneGOs.Start();
    }

    void OnDestroy()
    {
        _harmony.UnpatchSelf();
    }

    bool Debug = true; // TODO: replace with system that I won't forget to toggle
    private void RefreshBundles()
    {
        // In hot reload, assembly moves to diff folder, hence debug flag 
        string pluginDir = Debug ? Path.Combine(Paths.PluginPath, "DEVELOPMENT-BUILD-Custom Levels Reborn") : Path.GetDirectoryName(Info.Location);
        string bundleDir = Path.Combine(pluginDir, "bundles");

        var bundleNames = Directory.GetFiles(bundleDir).Select(Path.GetFileName);

        // Unload old
        foreach (var existingBundle in AssetBundle.GetAllLoadedAssetBundles())
        {
            if (bundleNames.Contains(existingBundle.name))
                existingBundle.Unload(true);
        }

        // Load in new
        foreach (var bundleName in bundleNames)
        {
            var bundle = AssetBundle.LoadFromFile(Path.Combine(bundleDir, bundleName));
            if (bundleName.EndsWith("_resources"))
            {
                // ResourceBundles.Add(bundleName, bundle);
                MapThumbnails.Add(bundleName, bundle.LoadAsset<Texture2D>("thumbnail"));
            }
            else if (bundle.isStreamedSceneAssetBundle)
                bundle.GetAllScenePaths()
                    .Select(Path.GetFileNameWithoutExtension)
                    .Do(ScenePaths.Add);
        }
    }
}