using System.Collections.Generic;
using System.IO;
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
    internal List<string> ScenePaths = [];

    Harmony _harmony = new(MyPluginInfo.PLUGIN_GUID);

    void Awake()
    {
        this.gameObject.hideFlags = HideFlags.HideAndDontSave;
        _harmony.PatchAll();
        RefreshBundles();
    }

    void OnDestroy()
    {
        _harmony.UnpatchSelf();
    }

    bool Debug = true; // TODO: replace with system that I won't forget to toggle
    List<string> BundleNames = ["test_map"];
    private void RefreshBundles()
    {
        // In hot reload, assembly moves to diff folder
        string bundlePath = Debug ? Path.Combine(Paths.PluginPath, "DEVELOPMENT-BUILD-Custom Levels Reborn") : Path.GetDirectoryName(Info.Location);

        // Unload old
        foreach (var existingBundle in AssetBundle.GetAllLoadedAssetBundles())
        {
            if (BundleNames.Contains(existingBundle.name))
                existingBundle.Unload(true);
        }

        // Load in new
        foreach (var bundleName in BundleNames)
        {
            var bundle = AssetBundle.LoadFromFile(Path.Combine(bundlePath, bundleName));
            foreach (var path in bundle.GetAllScenePaths())
            {
                ScenePaths.Add(path);
            }
        }
    }
}