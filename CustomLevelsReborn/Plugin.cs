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
        LoadBundles();

        gameObject.AddComponent<SyncMaps>();

        SceneManager.sceneLoaded += StealSceneGOs.OnSceneLoad;
    }

    void OnDestroy()
    {
        _harmony.UnpatchSelf();
        gameObject.GetComponent<SyncMaps>().UnAwake();
    }

    void LoadBundles()
    {
        var shared = AssetBundle.LoadFromFile(Path.Combine(PluginDir, "clr_shared")); // Potentially move to dynBundleLoad to avoid keeping in memory, tho the file is current of an inconsequential size
        MapDisabledSprite = shared.LoadAsset<Sprite>("MapDisableOverlay");
        SwapShadersAndTextures(shared);

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
                            SwapShadersAndTextures(bundle);
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
    /// Textures are swapped to save on file size for both the map kit and bundled maps.
    /// </summary>
    /// <param name="bundle"></param>
    void SwapShadersAndTextures(AssetBundle bundle)
    {
        HashSet<string> texturesToReplace = [];
        Dictionary<string, List<Material>> texturesToReplaceMap = [];

        foreach (var mat in bundle.LoadAllAssets<Material>())
        {
            foreach (var propertyName in mat.GetTexturePropertyNames())
            {
                var tex = mat.GetTexture(propertyName);
                if (tex && tex.name.EndsWith("_placeholder"))
                {
                    var nonPlaceholderName = tex.name[..^12];
                    texturesToReplace.Add(nonPlaceholderName);
                    if (!texturesToReplaceMap.ContainsKey(nonPlaceholderName))
                        texturesToReplaceMap.Add(nonPlaceholderName, []);
                    texturesToReplaceMap[nonPlaceholderName].Add(mat);
                }
            }

            var existingShader = mat.shader;

            var inGameShader = Shader.Find(existingShader.name);
            if (inGameShader)
            {
                mat.shader = inGameShader;
                Debug.LogError("replacing " + existingShader.name + " shader");
            }
        }

        foreach (var tex in Resources.FindObjectsOfTypeAll<Texture>())
        {
            Debug.LogError(tex.name);
            if (texturesToReplace.Contains(tex.name))
            {
                texturesToReplaceMap[tex.name].Do(mat => mat.SetTexture(tex.name, tex));
                Debug.LogError("replacing " + tex.name);
            }
        }
    }
}