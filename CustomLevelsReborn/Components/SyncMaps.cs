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
        MyceliumNetwork.RegisterLobbyDataKey("GameStarted");
        MyceliumNetwork.LobbyCreated += ResetMapLists;
        // MyceliumNetwork.LobbyLeft += OnLobbyLeave;

        SceneManager.sceneLoaded += ResetLobbyKey;
    }

    void ResetLobbyKey(Scene scene, LoadSceneMode _)
    {
        if (MyceliumNetwork.IsHost && MyceliumNetwork.InLobby && scene.name == "MainMenu")
        {
            MyceliumNetwork.SetLobbyData("GameStarted", false);
        }
    }

    void ResetMapLists()
    {
        mapsToDisable.Clear();
        customMapsInRotation.Clear();
        MyceliumNetwork.SetLobbyData("GameStarted", false);
    }

    [HarmonyPatch(typeof(SteamLobby), "OnLobbyEntered")]
    static class HandleLobbyEnter
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var setClientAddress = AccessTools.Method(typeof(Transport), "SetClientAddress");
            var determineMidMatchJoin = AccessTools.Method(typeof(SyncMaps), "DetermineMidMatchJoin");

            return new CodeMatcher(instructions)
            .MatchForward(useEnd: true,
            new CodeMatch(OpCodes.Callvirt, setClientAddress),
            new CodeMatch(OpCodes.Ldarg_0))
            .Insert(
                new CodeInstruction(OpCodes.Call, determineMidMatchJoin),
                new CodeInstruction(OpCodes.Ret))
            .InstructionEnumeration();
        }
    }

    static void DetermineMidMatchJoin()
    {
        if (MyceliumNetwork.IsHost || !MyceliumNetwork.GetLobbyData<bool>("GameStarted"))
        {
            if (SteamLobby.Instance._fishySteamworks.StartConnection(server: false))
            {
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
            var maps = MyceliumNetwork.GetLobbyData<string>("MapsInRotation").Split(";");
            if (maps.ToHashSet().SetEquals(CLRPlugin.MapVersions.ToHashSet()))
            {
                PauseManager.Instance.WriteOfflineLog("You are missing maps currently being used in this lobby!");
                SteamLobby.Instance.LeaveLobby();
            }
        }
    }

    [CustomRPC]
    void DisableNonSharedMaps(string clientMapString, RPCInfo sender)
    {
        var nonShared = CLRPlugin.MapVersions.Except(clientMapString.Split(";;")).ToArray();
        if (nonShared.Length > 0)
        {
            if (SceneMotor.Instance.currentSceneName == null)
                PauseManager.Instance.ShowInfoPopup($"{SteamFriends.GetFriendPersonaName(sender.SenderSteamID)} is missing {string.Join(", ", nonShared)}!");

            var versionStrippedMaps = nonShared.Select(map => map.Substring(0, map.LastIndexOf("-")));
            mapsToDisable.AddRange(versionStrippedMaps);
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

            MyceliumNetwork.SetLobbyData("MapsInRotation", string.Join(";", customMapsInRotation));
            MyceliumNetwork.SetLobbyData("GameStarted", true);
            customMapsInRotation.Clear();
        }
    }

    // RE-enabling maps would require keeping track of what players are blocking what maps
    // void OnLobbyLeave()
    // {

    // }

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