using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using FishNet.Transporting;
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

    internal void Awake()
    {
        MyceliumNetwork.RegisterNetworkObject(this, ID);

        MyceliumNetwork.RegisterLobbyDataKey("MapsInRotation");
        MyceliumNetwork.LobbyCreated += ResetMapLists;
        // MyceliumNetwork.LobbyLeft += OnLobbyLeave;
    }

    void ResetMapLists()
    {
        mapsToDisable.Clear();
        customMapsInRotation.Clear();
    }

    void DetermineMidMatchJoin()
    {
        if (MyceliumNetwork.IsHost || SceneMotor.Instance == null)
        {
            if (SteamLobby.Instance._fishySteamworks.StartConnection(server: false))
            {
                Debug.LogError("lobby join");
                SteamLobby.Instance.inSteamLobby = true;
                TargetedRPC(MyceliumNetwork.LobbyHost, "DisableNonSharedMaps", [string.Join(";;", CLRPlugin.MapVersions)]);
            }
            else
            {
                Debug.LogError("Failed to start FishySteamworks connection");
                SteamLobby.Instance.LeaveLobby();
            }
        }
        else
        {
            Debug.LogError("mid game join");
            var maps = MyceliumNetwork.GetLobbyData<string>("MapsInRotation").Split(";");
            if (maps != CLRPlugin.MapVersions.ToArray())
            {
                PauseManager.Instance.ShowInfoPopup("You are missing maps currently being used in this lobby!");
                SteamLobby.Instance.LeaveLobby();
            }
            // TargetedRPC(MyceliumNetwork.LobbyHost, "EvalMidMatchJoin", [string.Join(";;", CLRPlugin.MapVersions)]);
        }
    }

    [HarmonyPatch(typeof(SteamLobby), "OnLobbyEntered")]
    static class HandleLobbyEnter
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var determineMidMatchJoin = AccessTools.Method(typeof(SyncMaps), "DetermineMidMatchJoin");

            return new CodeMatcher(instructions)
            .MatchForward(useEnd: true,
            new CodeMatch(OpCodes.Callvirt, determineMidMatchJoin),
            new CodeMatch(OpCodes.Ldarg_0))
            .Insert(
                new CodeInstruction(OpCodes.Ret))
            .InstructionEnumeration();
        }
    }

    // RE-enabling maps would require keeping track of what players are blocking what maps
    // void OnLobbyLeave()
    // {

    // }

    [CustomRPC]
    void DisableNonSharedMaps(string clientMapString, RPCInfo sender)
    {
        Debug.LogError("host: received disable rpc");
        var nonShared = CLRPlugin.MapVersions.Except(clientMapString.Split(";;")).ToArray();
        if (nonShared.Length > 0)
        {
            PauseManager.Instance.ShowInfoPopup($"{SteamFriends.GetFriendPersonaName(sender.SenderSteamID)} is missing {string.Join(", ", nonShared)}!");
            mapsToDisable.AddRange(nonShared);
        }
    }

    // [CustomRPC]
    // void EvalMidMatchJoin(string clientMapString, RPCInfo sender)
    // {
    //     Debug.LogError("host: determining mid match join eligibility");
    //     var nonShared = customMapsInRotation.Except(clientMapString.Split(";;")).ToArray();
    //     var allowJoin = nonShared.Length == 0;
    //     TargetedRPC(sender.SenderSteamID, "AllowMidMatchJoin", [allowJoin]);
    // }

    // [CustomRPC]
    // void AllowMidMatchJoin(bool isAllowed)
    // {
    //     Debug.LogError("client: receiving mid match join eligibility");
    //     if (isAllowed)
    //     {
    //         JoinFishnet();
    //     }
    //     else
    //     {
    //         PauseManager.Instance.ShowInfoPopup("Failed to join lobby: Your custom map list is incompatible with the one being played.");
    //         SteamLobby.Instance.LeaveLobby();
    //         return;
    //     }
    // }

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

            MyceliumNetwork.SetLobbyData("MapsInRotation", string.Join(";", customMapsInRotation));
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