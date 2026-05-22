using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImportGameObjects : MonoBehaviour
{
    void Awake()
    {
        foreach (var go in StealSceneGOs.SceneGOs)
        {
            Object.Instantiate(go).transform.SetParent(transform);
        }
    }
}
