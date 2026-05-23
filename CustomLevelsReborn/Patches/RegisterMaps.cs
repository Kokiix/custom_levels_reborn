using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using BepInEx;
using ComputerysModdingUtilities;
using CustomLevelsReborn;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(MapsManager), "InitMaps")]
static class RegisterScenes
{
    // Technically repeats some work compared to transpiler but like.... look at the source code...
    static void Postfix(MapsManager __instance)
    {
        var customMapList = new List<Map>();

        var idx = __instance.allMaps.Length;
        foreach (var path in CLRPlugin.ScenePaths)
        {
            var map = new Map
            {
                index = idx++,
                mapName = path,
                isDlcExclusive = false,
                isAltMap = path.EndsWith("_alt"),
                isSelected = false,
                isUnlocked = true,
                mapInstance = null
            };
            customMapList.Add(map);
            __instance.allMapsDict.Add(map.mapName, map);
            GameObject gameObject = Object.Instantiate(__instance.mapInstance, __instance.standardMapParent.position, Quaternion.identity, __instance.standardMapParent);
            map.mapInstance = gameObject.GetComponent<MapInstance>();
            map.mapInstance.name = map.mapName;
            map.mapInstance.selected = map.isSelected;
        }

        __instance.SortMapsFromMapInstanceName();
        __instance.allMaps = [.. __instance.allMaps, .. customMapList];
        __instance.unlockedMaps = [.. __instance.unlockedMaps, .. customMapList.Select(map => map.index)];
    }
}

[HarmonyPatch(typeof(MapInstance), "Start")]
static class SetThumbnail
{
    // Similar to above, technically unnecessary UI reload bc I'm lazy
    public static void Postfix(MapInstance __instance)
    {
        if (!__instance.sprite)
        {
            __instance.sprite = CLRPlugin.MapThumbnails[__instance.name.ToLower() + "_resources"];
            __instance.img.texture = __instance.sprite;
            __instance.UpdateUI();
        }
    }
}