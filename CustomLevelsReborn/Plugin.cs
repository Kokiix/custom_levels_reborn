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
    internal List<string> ScenePaths = new();

    Harmony _harmony = new(MyPluginInfo.PLUGIN_GUID);

    void Awake()
    {
        _harmony.PatchAll();
        this.gameObject.hideFlags = HideFlags.HideAndDontSave;


    }

    void OnDestroy()
    {
        _harmony.UnpatchSelf();
    }

    bool Debug = true; // TODO: replace with system that I won't forget to toggle
    List<string> BundleNames = ["test_map"];
    private void LoadBundles()
    {
        string bundlePath = Debug ? Path.Combine(Paths.PluginPath, "DEVELOPMENT-BUILD-Custom Levels Reborn") : Path.GetDirectoryName(Info.Location);

        // var sharedAssets = AssetBundle.LoadFromFile(Path.Combine(bundlePath, "shared"));
        // foreach (var material in sharedAssets.LoadAllAssets<Material>())
        //     material.shader = Shader.Find(material.shader.name);
        // SharedBundle = sharedAssets;
        foreach (var filePath in Directory.GetFiles(bundlePath))
        {
            var fileName = Path.GetFileName(filePath);
            if (!BundleNames.Contains(fileName) || fileName == "shared") continue;

            var bundle = AssetBundle.LoadFromFile(Path.Combine(bundlePath, fileName));
            foreach (var path in bundle.GetAllScenePaths())
            {
                ScenePaths.Add(path);
            }
        }
    }
}