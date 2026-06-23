using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

static class FixKillCamNRE
{
    [HarmonyPatch(typeof(KillCam), "OnDisable")]
    static class PreventDeathExplosion
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var switchCamText = AccessTools.Field(typeof(KillCam), "switchCamText");
            var areFieldsUninitialized = AccessTools.Method(typeof(FixKillCamNRE), "AreFieldsUninitialized");

            return new CodeMatcher(instructions, generator)
            .End()
            .CreateLabel(out Label ret)
            .MatchBack(useEnd: false,
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Ldfld, switchCamText))
            .Insert(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, areFieldsUninitialized),
                new CodeInstruction(OpCodes.Brtrue, ret))
            .InstructionEnumeration();
        }
    }

    static bool AreFieldsUninitialized(KillCam __instance)
    {
        return !__instance.switchCamText && !__instance.playerHpText && !__instance.playerNameText;
    }
}