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

    static string PluginDir;

    void Awake()
    {
        Log = Logger;
        this.gameObject.hideFlags = HideFlags.HideAndDontSave;
        _harmony.PatchAll();

        var isKokiPC = Path.GetDirectoryName(Info.Location).StartsWith(@"C:\Users\koki\AppData\Roaming");
        PluginDir = isKokiPC ? Path.Combine(Paths.PluginPath, "DEVELOPMENT-BUILD-Custom Levels Reborn") : Path.GetDirectoryName(Info.Location);
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
        var shared = AssetBundle.LoadFromFile(Path.Combine(PluginDir, "shared")); // Potentially move to dynBundleLoad, tho the file is currently microscopic in size
        SwapShaders(shared);

        foreach (var modDir in Directory.EnumerateDirectories(Paths.PluginPath))
        {
            foreach (var folder in Directory.EnumerateDirectories(modDir))
            {
                if (folder.EndsWith("CustomMaps"))
                {
                    var pluginVersion = GetPluginVersion(modDir);

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
                                MapVersions.Add(scene + "-v" + pluginVersion);
                            }

                            bundle.Unload(true);
                        }
                        else if (filePath.EndsWith("_resources"))
                        {
                            SwapShaders(bundle);
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

    /// <summary>
    /// Shaders compile differently depending on if the bundle target is set to Windows or Linux, because of Vulkan vs OpenGL. 
    /// Swapping shaders at runtime is an lazy way to fix it :p
    /// </summary>
    /// <param name="bundle"></param>
    void SwapShaders(AssetBundle bundle)
    {
        foreach (var mat in bundle.LoadAllAssets<Material>())
        {
            var existingShader = mat.shader;
            var inGameShader = Shader.Find(existingShader.name);
            if (inGameShader)
            {
                mat.shader = inGameShader;
            }
        }
    }
}