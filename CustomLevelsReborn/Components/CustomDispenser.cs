using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CustomDispenser : MonoBehaviour
{
    public WeaponType[] ItemsToSpawn = [];

    void Awake()
    {
        // Required for Recon mode
        if (SpawnerManager.NameToWeaponDict.Count == 0)
        {
            SpawnerManager.PopulateAllWeapons();
        }

        var dispenser = gameObject.GetComponent<ItemDispenser>();
        dispenser.itemsToSpawn = ItemsToSpawn
        .Select(wepType => wepType.ToString() == "AKK" ? "AK-K" : wepType.ToString())
        .Select(wepString => SpawnerManager.NameToWeaponDict[wepString])
        .ToArray();
    }
}
