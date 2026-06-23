using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

// This is a vanilla bug that triggers on all dispensers.. doesn't seem to have any gameplay impact tho

static class FixItemDispenseAudioNRE
{
    [HarmonyPatch(typeof(ItemBehaviour), "Awake")]
    static class InitAudioBeforeCollision
    {
        static void Prefix(ItemBehaviour __instance)
        {
            var audio = __instance.GetComponent<AudioSource>();
            __instance.audio = audio;
        }
    }
}