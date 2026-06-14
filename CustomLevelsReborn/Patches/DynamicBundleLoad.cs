using FishNet.Managing.Scened;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

static class BundleLoad
{
    static AssetBundle bundleRef;

    internal static void UnloadBundle(Scene scene)
    {
        if (bundleRef && CLRPlugin.SceneToBundleDir.ContainsKey(scene.name))
        {
            LightmapSettings.lightmaps = new LightmapData[0];
            bundleRef.Unload(true);
            bundleRef = null;
        }
    }

    // Gets triggered multiple times on sceneload i think..
    internal static void Start(string sceneName)
    {
        if (!bundleRef && CLRPlugin.SceneToBundleDir.ContainsKey(sceneName))
        {
            bundleRef = AssetBundle.LoadFromFile(CLRPlugin.SceneToBundleDir[sceneName]);
        }
    }
}

static class MultiplayerBundleLoad
{
    [HarmonyPatch(typeof(SceneMotor), "GetNextMap")]
    static class LoadHost
    {
        static void Prepare()
        {
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded += BundleLoad.UnloadBundle;
        }

        static void Postfix(string __result)
        {
            BundleLoad.Start(__result);
        }
    }

    [HarmonyPatch(typeof(DefaultSceneProcessor), "BeginLoadAsync")]
    static class LoadClient
    {
        static void Prefix(string sceneName)
        {
            BundleLoad.Start(sceneName);
        }
    }
}

[HarmonyPatch(typeof(SceneMotor), "RpcLogic___EnterScene_3615296227")]
static class SingleplayerBundleLoad
{
    static void Prefix(string sceneName)
    {
        BundleLoad.Start(sceneName);
    }
}