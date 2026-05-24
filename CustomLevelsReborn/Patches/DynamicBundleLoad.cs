using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using BepInEx;
using ComputerysModdingUtilities;
using CustomLevelsReborn;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(SceneMotor), "RpcLogic___EnterScene_3615296227")]
static class DynBundleLoad
{
    static void Prefix(SceneMotor __instance, string sceneName, ref AssetBundle __state)
    {
        if (CLRPlugin.SceneToBundleDir.ContainsKey(sceneName))
        {
            __state = AssetBundle.LoadFromFile(CLRPlugin.SceneToBundleDir[sceneName]);
        }
    }

    static void Postfix(SceneMotor __instance, string sceneName, ref AssetBundle __state)
    {
        if (__state)
        {
            __state.UnloadAsync(false);
        }
    }
}