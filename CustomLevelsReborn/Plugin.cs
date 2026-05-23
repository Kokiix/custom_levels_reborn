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

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class CLRPlugin : BaseUnityPlugin
{
    internal static ManualLogSource Log;
    internal static List<string> ScenePaths = [];
    internal static Dictionary<string, Texture2D> MapThumbnails = [];
    // internal static Dictionary<string, AssetBundle> ResourceBundles = [];

    Harmony _harmony = new(MyPluginInfo.PLUGIN_GUID);

    void Awake()
    {
        Log = Logger;
        this.gameObject.hideFlags = HideFlags.HideAndDontSave;
        _harmony.PatchAll();

        RefreshBundles();
        SceneManager.sceneLoaded += StealSceneGOs.OnSceneLoad;
    }

    void OnDestroy()
    {
        _harmony.UnpatchSelf();
    }

    private void RefreshBundles()
    {
        string myPluginDir = Path.GetDirectoryName(Info.Location);
        AssetBundle.LoadFromFile(Path.Combine(myPluginDir, "shared"));

        foreach (var pluginDir in Directory.EnumerateDirectories(Paths.PluginPath))
        {
            foreach (var folder in Directory.EnumerateDirectories(pluginDir))
            {
                if (folder.EndsWith("CustomMaps"))
                {
                    foreach (var fileName in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
                    {
                        var bundle = AssetBundle.LoadFromFile(fileName);
                        if (bundle.isStreamedSceneAssetBundle)
                        {
                            bundle.GetAllScenePaths()
                            .Select(Path.GetFileNameWithoutExtension)
                            .Do(ScenePaths.Add);
                        }
                        else if (fileName.EndsWith("_resources"))
                        {
                            MapThumbnails.Add(Path.GetFileName(fileName), bundle.LoadAsset<Texture2D>("thumbnail"));
                        }
                    }
                }
            }
        }
    }
}