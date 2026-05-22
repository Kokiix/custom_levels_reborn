using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

static class StealSceneGOs
{
    static readonly string MapName = "TheSamePlace_08";
    static internal List<GameObject> SceneGOs = [];
    static internal void Start()
    {
        SceneManager.sceneLoaded += GetSceneGOs;
        SceneManager.LoadSceneAsync(MapName, LoadSceneMode.Additive);
        SceneManager.UnloadSceneAsync(MapName);
        SceneManager.sceneLoaded -= GetSceneGOs;
    }

    static void GetSceneGOs(Scene s, LoadSceneMode mode)
    {
        Debug.LogError(s.name);
        if (mode != LoadSceneMode.Additive || s.name != MapName) return;


        Debug.LogError("found!");
    }
}