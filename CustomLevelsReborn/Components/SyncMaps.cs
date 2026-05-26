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

    static Dictionary<string, List<CSteamID>> mapsToDisable = [];
    static List<string> customMapsInRotation = [];

    internal void Awake()
    {
        MyceliumNetwork.RegisterNetworkObject(this, ID);

        MyceliumNetwork.RegisterLobbyDataKey("MapsInRotation");
        MyceliumNetwork.RegisterLobbyDataKey("GameStarted");
        MyceliumNetwork.LobbyCreated += ResetMapLists;
        MyceliumNetwork.LobbyLeft += OnLobbyLeave;

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

    string[] GetNonSharedMaps(string input)
    {
        return CLRPlugin.MapVersions
        .Except(input.Split(";;"))
        .Select(map => map.Substring(0, map.LastIndexOf("-")))
        .ToArray();
    }

    [CustomRPC]
    void DisableNonSharedMaps(string clientMapString, RPCInfo sender)
    {
        var nonShared = GetNonSharedMaps(clientMapString);
        foreach (var map in nonShared)
        {
            if (!mapsToDisable.TryGetValue(map, out List<CSteamID> IDs))
            {
                mapsToDisable.Add(map, new List<CSteamID>());
            }

            IDs.Add(sender.SenderSteamID);
        }

        if (nonShared.Length > 0 && (SceneMotor.Instance.currentSceneName == null || SceneMotor.Instance.testMap))
            PauseManager.Instance.ShowInfoPopup($"{SteamFriends.GetFriendPersonaName(sender.SenderSteamID)} is missing {string.Join(", ", nonShared)}!");
    }

    [CustomRPC]
    void EnableSharedMaps(string clientMapString, RPCInfo sender)
    {
        var enabledMaps = new List<string>();
        foreach (var map in GetNonSharedMaps(clientMapString))
        {
            if (mapsToDisable.TryGetValue(map, out List<CSteamID> IDs))
            {
                IDs.Remove(sender.SenderSteamID);
                if (IDs.Count == 0)
                {
                    mapsToDisable.Remove(map);
                    enabledMaps.Add(map);
                }
            }
        }
        if (enabledMaps.Count > 0 && SceneMotor.Instance.currentSceneName == null || SceneMotor.Instance.testMap)
            PauseManager.Instance.ShowInfoPopup($"{SteamFriends.GetFriendPersonaName(sender.SenderSteamID)} has left, enabling {string.Join(", ", enabledMaps)}!");
    }

    [HarmonyPatch(typeof(SceneMotor), "ServerStartGameScene")]
    static class DisableMapsOnGameStart
    {
        static void Prefix(SceneMotor __instance)
        {
            __instance.PlayListMaps.RemoveAll(map =>
            {
                if (mapsToDisable.Keys.Contains(map))
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

    void OnLobbyLeave()
    {
        TargetedRPC(MyceliumNetwork.LobbyHost, "EnableSharedMaps", [string.Join(";;", CLRPlugin.MapVersions)]);
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