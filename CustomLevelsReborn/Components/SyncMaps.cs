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
        MyceliumNetwork.LobbyLeft += OnLobbyLeave;
    }

    void OnLobbyJoin()
    {

    }

    void OnLobbyLeave()
    {

    }

    [HarmonyPatch(typeof(SceneMotor), "ServerStartGameScene")]
    static class DisableMapsOnGameStart
    {
        static void Prefix(SceneMotor __instance)
        {
            __instance.PlayListMaps.RemoveAll(map =>
            {
                return false;
            });
        }
    }

    // static void RPC(string methodname, object[] parameters)
    // {
    //     MyceliumNetwork.RPC(
    //         ID,
    //         methodname,
    //         ReliableType.Reliable,
    //         parameters
    //     );
    // }
}