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

        MyceliumNetwork.LobbyCreated += ResetMapLists;
        MyceliumNetwork.LobbyEntered += OnLobbyJoin;
        // MyceliumNetwork.LobbyLeft += OnLobbyLeave;
    }

    void ResetMapLists()
    {
        mapsToDisable.Clear();
        customMapsInRotation.Clear();
    }

    void JoinFishnet()
    {
        Debug.LogError("manually join fishnet");
        if (SteamLobby.Instance._fishySteamworks.StartConnection(server: false))
        {
            SteamLobby.Instance.inSteamLobby = true;
        }
        else
        {
            Debug.LogError("CONNECTION FAILED");
            SteamLobby.Instance.LeaveLobby();
        }
    }

    [HarmonyPatch(typeof(FishySteamworks.FishySteamworks), "StartConnection")]
    static class Test
    {
        static void Postfix(bool server)
        {
            Debug.LogError("fishnet connect from " + server);
        }
    }

    [HarmonyPatch(typeof(SteamLobby), "OnLobbyEntered")]
    static class BlockDefaultMidMatchJoin
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var setClientAddress = AccessTools.Field(typeof(Transport), "SetClientAddress");

            return new CodeMatcher(instructions)
            .MatchForward(useEnd: true,
            new CodeMatch(OpCodes.Callvirt, setClientAddress),
            new CodeMatch(OpCodes.Ldarg_0))
            .Insert(
                new CodeInstruction(OpCodes.Ret))
            .InstructionEnumeration();
        }
    }

    void OnLobbyJoin()
    {
        if (MyceliumNetwork.IsHost)
        {
            return;
        }

        Debug.LogError("joining");
        if (SceneMotor.Instance.currentSceneName == null)
        {
            Debug.LogError("lobby join");
            JoinFishnet();
            TargetedRPC(MyceliumNetwork.LobbyHost, "DisableNonSharedMaps", [string.Join(";;", CLRPlugin.MapVersions)]);
        }
        else
        {
            Debug.LogError("mid game join");
            TargetedRPC(MyceliumNetwork.LobbyHost, "EvalMidMatchJoin", [string.Join(";;", CLRPlugin.MapVersions)]);
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
            CLRPlugin.Log.LogWarning($"{SteamFriends.GetFriendPersonaName(sender.SenderSteamID)} is missing {string.Join(", ", nonShared)}!");
            mapsToDisable.AddRange(nonShared);
        }
    }

    [CustomRPC]
    void EvalMidMatchJoin(string clientMapString, RPCInfo sender)
    {
        Debug.LogError("host: determining mid match join eligibility");
        var nonShared = customMapsInRotation.Except(clientMapString.Split(";;")).ToArray();
        var allowJoin = nonShared.Length == 0;
        TargetedRPC(sender.SenderSteamID, "AllowMidMatchJoin", [allowJoin]);
    }

    [CustomRPC]
    void AllowMidMatchJoin(bool isAllowed)
    {
        Debug.LogError("client: receiving mid match join eligibility");
        if (isAllowed)
        {
            JoinFishnet();
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