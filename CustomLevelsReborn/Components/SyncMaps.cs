using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using MyceliumNetworking;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;

class SyncMaps : MonoBehaviour
{
    static readonly uint ID = 568324179;

    static List<string> mapsToDisable = [];
    static List<string> customMapsInRotation = [];
    static bool allowMidMatchJoin = false;

    internal void Awake()
    {
        MyceliumNetwork.RegisterNetworkObject(this, ID);

        MyceliumNetwork.LobbyCreated += ResetMapLists;
        MyceliumNetwork.LobbyEntered += OnLobbyJoin;
        // MyceliumNetwork.LobbyLeft += OnLobbyLeave;
    }

    void ResetMapLists()
    {
        mapsToDisable.Clear();
        customMapsInRotation.Clear();
    }

    void OnLobbyJoin()
    {
        if (MyceliumNetwork.IsHost)
        {
            return;
        }

        if (SceneMotor.Instance.currentSceneName == null)
        {
            allowMidMatchJoin = true;
            SteamLobby.Instance.OnLobbyEntered(savedCallback);
            TargetedRPC(MyceliumNetwork.LobbyHost, "DisableNonSharedMaps", [string.Join(";;", CLRPlugin.MapVersions)]);
        }
        else
            TargetedRPC(MyceliumNetwork.LobbyHost, "EvalMidMatchJoin", [string.Join(";;", CLRPlugin.MapVersions)]);
    }

    // RE-enabling maps would require keeping track of what players are blocking what maps
    // void OnLobbyLeave()
    // {

    // }

    [CustomRPC]
    void DisableNonSharedMaps(string clientMapString, RPCInfo sender)
    {
        var nonShared = CLRPlugin.MapVersions.Except(clientMapString.Split(";;")).ToArray();
        if (nonShared.Length > 0)
        {
            CLRPlugin.Log.LogWarning($"{SteamFriends.GetFriendPersonaName(sender.SenderSteamID)} is missing {string.Join(", ", nonShared)}!");
            mapsToDisable.AddRange(nonShared);
        }
    }

    [CustomRPC]
    void EvalMidMatchJoin(string clientMapString, RPCInfo sender)
    {
        var nonShared = customMapsInRotation.Except(clientMapString.Split(";;")).ToArray();
        var allowJoin = nonShared.Length == 0;
        TargetedRPC(sender.SenderSteamID, "AllowMidMatchJoin", [allowJoin]);
    }

    [CustomRPC]
    void AllowMidMatchJoin(bool isAllowed)
    {
        if (isAllowed)
        {
            allowMidMatchJoin = true;
            SteamLobby.Instance.OnLobbyEntered(savedCallback);
        }
        else
        {
            PauseManager.Instance.ShowInfoPopup("Failed to join lobby: Your custom map list is incompatible with the one being played.");
            SteamLobby.Instance.LeaveLobby();
            return;
        }
    }

    [HarmonyPatch(typeof(SceneMotor), "ServerStartGameScene")]
    static class DisableMapsOnGameStart
    {
        static void Prefix(SceneMotor __instance)
        {
            __instance.PlayListMaps.RemoveAll(map =>
            {
                if (mapsToDisable.Contains(map))
                {
                    return true;
                }
                else if (CLRPlugin.SceneToBundleDir.Keys.Contains(map))
                {
                    customMapsInRotation.Add(map);
                }

                return false;
            });
        }
    }

    static LobbyEnter_t savedCallback;
    [HarmonyPatch(typeof(SteamLobby), "OnLobbyEntered")]
    static class BlockDefaultMidMatchJoin
    {

        static bool IsJoinAllowed()
        {
            if (MyceliumNetwork.IsHost) return true;
            return allowMidMatchJoin;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var isJoinAllowed = AccessTools.Method(typeof(BlockDefaultMidMatchJoin), "IsJoinAllowed");

            return new CodeMatcher(instructions, generator)
            .End().CreateLabel(out Label ret)
            .Start()
            .Insert(
                new CodeInstruction(OpCodes.Call, isJoinAllowed),
                new CodeInstruction(OpCodes.Brfalse, ret))
            .InstructionEnumeration();
        }

        static void Postfix(LobbyEnter_t callback)
        {
            savedCallback = callback;
            allowMidMatchJoin = false;
        }
    }

    static void TargetedRPC(CSteamID target, string methodname, object[] parameters)
    {
        MyceliumNetwork.RPCTarget(
            ID,
            methodname,
            target,
            ReliableType.Reliable,
            parameters
        );
    }
}