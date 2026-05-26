using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using FishNet.Transporting;
using HarmonyLib;
using MyceliumNetworking;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

class SyncMaps : MonoBehaviour
{
    static readonly uint ID = 568324179;

    static Dictionary<string, List<CSteamID>> mapsToDisable = [];
    static List<string> customMapsInRotation = [];

    void Awake()
    {
        MyceliumNetwork.DeregisterNetworkObject(this, ID);
        MyceliumNetwork.RegisterNetworkObject(this, ID);
        MyceliumNetwork.RegisterLobbyDataKey("MapsInRotation");
        MyceliumNetwork.RegisterLobbyDataKey("GameStarted");
        MyceliumNetwork.LobbyCreated += ResetMapLists;
        MyceliumNetwork.PlayerLeft += OnLobbyLeave;
        SceneManager.sceneLoaded += ResetLobbyKey;
    }

    internal void UnAwake()
    {
        MyceliumNetwork.LobbyCreated -= ResetMapLists;
        MyceliumNetwork.PlayerLeft -= OnLobbyLeave;
        SceneManager.sceneLoaded -= ResetLobbyKey;
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

    static void Connect()
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

    static void DetermineMidMatchJoin()
    {
        if (MyceliumNetwork.IsHost || !MyceliumNetwork.GetLobbyData<bool>("GameStarted"))
        {
            Connect();
        }
        else
        {
            CLRPlugin.MapVersions.Do(Debug.LogError);
            var currMaps = MyceliumNetwork.GetLobbyData<string>("MapsInRotation").Split(";");
            if (currMaps.Except(CLRPlugin.MapVersions).Any())
            {
                PauseManager.Instance.WriteOfflineLog("You are missing maps currently being used in this lobby!");
            }
            else
            {
                Connect();
                return;
            }
            SteamLobby.Instance.LeaveLobby();
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
                IDs = new List<CSteamID>();
                mapsToDisable.Add(map, IDs);
            }
            IDs.Add(sender.SenderSteamID);
        }

        foreach (var pair in CLRPlugin.MapDisabledSprites)
        {
            if (nonShared.Contains(pair.Key))
            {
                pair.Value.SetActive(true);
            }
        }

        if (nonShared.Length > 0 && (SceneMotor.Instance.currentSceneName == null || !SceneMotor.Instance.testMap))
            PauseManager.Instance.ShowInfoPopup($"{SteamFriends.GetFriendPersonaName(sender.SenderSteamID)} is missing {string.Join(", ", nonShared)}!");
    }

    [HarmonyPatch(typeof(MapSelection), "UpdateScenes")]
    static class BlockDisabledMapsFromQueue
    {
        static void Prefix(MapSelection __instance)
        {
            foreach (var scene in __instance.sceneInstances)
            {
                if (mapsToDisable.Keys.Contains(scene.sceneName))
                {
                    scene.selected = false;
                }
            }
        }
    }

    [HarmonyPatch(typeof(SceneMotor), "ServerStartGameScene")]
    static class SetMapsInRotation
    {
        static void Postfix(SceneMotor __instance)
        {
            __instance.PlayListMapsQueue
            .Where(CLRPlugin.SceneToBundleDir.Keys.Contains)
            .Do(map =>
            {
                foreach (var versionedMap in CLRPlugin.MapVersions)
                {
                    if (versionedMap.Substring(0, versionedMap.LastIndexOf("-")) == map)
                        customMapsInRotation.Add(versionedMap);
                }
            });

            MyceliumNetwork.SetLobbyData("MapsInRotation", string.Join(";", customMapsInRotation));
            MyceliumNetwork.SetLobbyData("GameStarted", true);
            customMapsInRotation.Clear();
        }
    }

    [HarmonyPatch(typeof(SteamLobby), "EnterExplorationMap")]
    static class BlockExploringDisabledMap
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var isMapDisabled = AccessTools.Method(typeof(SyncMaps), "IsMapDisabled");

            return new CodeMatcher(instructions, generator)
            .End().CreateLabel(out Label ret)
            .Start().Insert(
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Call, isMapDisabled),
                new CodeInstruction(OpCodes.Brtrue, ret))
            .InstructionEnumeration();
        }
    }

    static bool IsMapDisabled(string mapname)
    {
        var disabled = mapsToDisable.Keys.Contains(mapname);
        if (disabled)
        {
            PauseManager.Instance.WriteOfflineLog("Someone in the lobby doesn't have this map");
        }
        return disabled;
    }

    void OnLobbyLeave(CSteamID id)
    {
        if (!MyceliumNetwork.IsHost || MyceliumNetwork.Players.Length == 0) return;

        var enabledMaps = new List<string>();
        foreach (var pair in mapsToDisable)
        {
            if (pair.Value.Contains(id))
            {
                pair.Value.Remove(id);
                if (pair.Value.Count == 0)
                {
                    enabledMaps.Add(pair.Key);
                    CLRPlugin.MapDisabledSprites[pair.Key].SetActive(false);
                }
            }
        }

        enabledMaps.Do(m => mapsToDisable.Remove(m));

        if (enabledMaps.Count > 0 && (SceneMotor.Instance.currentSceneName == null || !SceneMotor.Instance.testMap))
            PauseManager.Instance.WriteOfflineLog($"{string.Join(", ", enabledMaps)} have been re-enabled.");
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