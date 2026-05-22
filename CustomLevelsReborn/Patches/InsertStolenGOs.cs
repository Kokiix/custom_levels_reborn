// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// using System.Reflection.Emit;
// using BepInEx;
// using ComputerysModdingUtilities;
// using CustomLevelsReborn;
// using HarmonyLib;
// using UnityEngine;

// [HarmonyPatch(typeof(PlayerHealth), "Awake")]
// static class InsertGOs
// {
//     static void Prefix(PlayerHealth __instance)
//     {
//         Debug.LogError("prefix");
//         if (!GameObject.Find("Main Camera"))
//         {
//             Debug.LogError("making camrea");
//             StealSceneGOs.SceneGOs.Do(go => Object.Instantiate(go));
//             Debug.LogError(GameObject.Find("Main Camera"));
//         }
//     }
// }