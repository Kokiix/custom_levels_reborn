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
    static class Load
    {
        static void Postfix(string __result)
        {
            if (CLRPlugin.SceneToBundleDir.ContainsKey(__result))
            {
                bundle = AssetBundle.LoadFromFile(CLRPlugin.SceneToBundleDir[__result]);
            }
        }
    }

    [HarmonyPatch(typeof(SceneManager), "LoadGlobalScenes")]
    static class Unload
    {
        static void Postfix()
        {
            if (bundle)
            {
                bundle.UnloadAsync(false);
            }

            bundle = null;
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