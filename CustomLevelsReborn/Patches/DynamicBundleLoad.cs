using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using BepInEx;
using ComputerysModdingUtilities;
using CustomLevelsReborn;
using FishNet.Managing.Scened;
using HarmonyLib;
using UnityEngine;

static class BundleLoad
{
    static AssetBundle bundleRef;
    static string lastScene;


    internal static void Start(string sceneName)
    {
        if (bundleRef)
        {
            bundleRef.Unload(false);
            bundleRef = null;
        }

        if (CLRPlugin.SceneToBundleDir.ContainsKey(sceneName))
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