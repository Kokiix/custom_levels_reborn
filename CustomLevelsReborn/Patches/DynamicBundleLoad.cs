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

static class MultiplayerBundleLoad
{
    static AssetBundle bundle;
    [HarmonyPatch(typeof(SceneMotor), "GetNextMap")]
    static class LoadHost
    {
        static void Postfix(string __result)
        {
            if (bundle)
            {
                bundle.UnloadAsync(false);
                bundle = null;
            }

            if (CLRPlugin.SceneToBundleDir.ContainsKey(__result))
            {
                bundle = AssetBundle.LoadFromFile(CLRPlugin.SceneToBundleDir[__result]);
            }
        }
    }

    [HarmonyPatch(typeof(DefaultSceneProcessor), "BeginLoadAsync")]
    static class LoadClient
    {
        static void Prefix(string sceneName)
        {
            if (bundle)
            {
                bundle.UnloadAsync(false);
                bundle = null;
            }

            if (CLRPlugin.SceneToBundleDir.ContainsKey(sceneName))
            {
                bundle = AssetBundle.LoadFromFile(CLRPlugin.SceneToBundleDir[sceneName]);
            }
        }
    }
}

[HarmonyPatch(typeof(SceneMotor), "RpcLogic___EnterScene_3615296227")]
static class SingleplayerBundleLoad
{
    static void Prefix(string sceneName, ref AssetBundle __state)
    {
        if (CLRPlugin.SceneToBundleDir.ContainsKey(sceneName))
        {
            __state = AssetBundle.LoadFromFile(CLRPlugin.SceneToBundleDir[sceneName]);
        }
    }

    static void Postfix(AssetBundle __state)
    {
        if (__state)
        {
            __state.UnloadAsync(false);
        }
    }
}