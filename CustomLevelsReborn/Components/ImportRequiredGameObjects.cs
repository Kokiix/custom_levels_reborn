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
            var newGO = Object.Instantiate(go);
            newGO.transform.SetParent(transform);
            newGO.SetActive(true);
            newGO.name = newGO.name.Replace("(Clone)", "");
        }
    }
}
