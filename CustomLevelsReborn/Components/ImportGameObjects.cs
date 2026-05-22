using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-500)]
public class ImportGameObjects : MonoBehaviour
{
    void Awake()
    {
        foreach (var go in StealSceneGOs.SceneGOs)
        {
            Object.Instantiate(go);
            go.SetActive(true);
            go.name = go.name.Replace("(Clone)", "");
        }
    }
}
