using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

static class StealSceneGOs
{
    static readonly string MapToStealFrom = "TheSamePlace_08";
    static readonly List<string> RequiredGOs = ["GameManager", "NetworkManager", "---USER INTERFACE---", "Main Camera"];
    internal static List<GameObject> SceneGOs = [];

    static bool stolen = false;
    internal static void OnSceneLoad(Scene s, LoadSceneMode mode)
    {
        if (s.name == "MainMenu")
        {
            SceneManager.LoadSceneAsync(MapToStealFrom, LoadSceneMode.Additive);
            stolen = true;
        }
        else if (s.name == MapToStealFrom)
        {
            CLRPlugin.Log.LogInfo("This error isn't that important :)");
            foreach (var obj in s.GetRootGameObjects())
            {
                if (RequiredGOs.Contains(obj.name))
                {
                    obj.SetActive(false);
                    var requiredObj = Object.Instantiate(obj);
                    Object.DontDestroyOnLoad(requiredObj);
                    SceneGOs.Add(requiredObj);
                    obj.SetActive(true);
                }
            }

            SceneManager.sceneLoaded -= OnSceneLoad;
            SceneManager.UnloadSceneAsync(MapToStealFrom);
        }
    }
}