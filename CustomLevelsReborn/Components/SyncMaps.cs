using System.Collections.Generic;
using System.Linq;
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

        MyceliumNetwork.LobbyEntered += OnLobbyJoin;
        // MyceliumNetwork.LobbyLeft += OnLobbyLeave;
    }

    void OnLobbyJoin()
    {
        Debug.LogError(SceneMotor.Instance.currentSceneName);
        if (MyceliumNetwork.IsHost) return;

        // if () // is in lobby
        //     TargetedRPC(MyceliumNetwork.LobbyHost, "DisableNonSharedMaps", [string.Join(",", CLRPlugin.MapVersions)]);
    }

    // void OnLobbyLeave()
    // {

    // }

    [CustomRPC]
    void DisableNonSharedMaps(string clientMapString, RPCInfo sender)
    {
        var nonShared = CLRPlugin.MapVersions.Except(clientMapString.Split(",")).ToArray();
        if (nonShared.Length > 0)
        {
            CLRPlugin.Log.LogWarning($"{SteamFriends.GetFriendPersonaName(sender.SenderSteamID)} is missing {string.Join(", ", nonShared)}!");
            mapsToDisable.AddRange(nonShared);
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
                    return false;
                }
                else if (CLRPlugin.SceneToBundleDir.Keys.Contains(map))
                {
                    customMapsInRotation.Add(map);
                }

                return true;
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