using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum WeaponType
{
    AAA12,
    AKK,
    APMine,
    AR15,
    BaseballBat,
    Bayshore,
    BeamLoad,
    Bender,
    BigFattyBro,
    BlankState,
    Bublee,
    Bukanee,
    Claymore,
    Couperet,
    Crisis,
    CurvedKnife,
    DF_Blister,
    DF_Cyst,
    DF_GodSword,
    DF_Torrent,
    Dispenser,
    DualLauncher,
    Elephant,
    FG42,
    Flamberge,
    FlashLight,
    Gamma,
    GammaGen2,
    GlaiveGun,
    GlandGrenade,
    Glock,
    Gun,
    Gust,
    HandCanon,
    HandGrenade,
    Havoc,
    Hill_H15,
    HK_Caws,
    HK_G11,
    Impetus,
    JahvalMahmaerd,
    Kanye,
    Katana,
    Keso,
    Kusma,
    M2000,
    Mac10,
    Minigun,
    Mortini,
    Nizeh,
    Nugget,
    Phoenix,
    Propeller,
    Prophet,
    ProximityMine,
    QCW05,
    Repulsar,
    Revolver,
    RocketLauncher,
    SawedOff,
    Shotgun,
    Silenzzio,
    SMG,
    SmithCarbine,
    StunGrenade,
    StunMine,
    Stylus,
    Taser,
    Tromblonj,
    Warden,
    Webley,
    Yangtse
}

public class CustomSpawner : MonoBehaviour
{
    [SerializeField] WeaponType _weapon;

    [Range(0f, 60f)]
    [SerializeField] float _weaponRespawnTimeInSeconds = 3f;

    void Awake()
    {
        var weaponString = _weapon.ToString();
        if (weaponString == "AKK")
            weaponString = "AK-K";

        if (SpawnerManager.NameToWeaponDict.Count == 0)
        {
            SpawnerManager.PopulateAllWeapons();
        }
        Debug.LogError(weaponString);
        Debug.LogError(SpawnerManager.NameToWeaponDict.Count);
        Debug.LogError(SpawnerManager.NameToWeaponDict[weaponString]);

        // var spawner = gameObject.AddComponent<ItemSpawner>();
        // spawner.itemToSpawn = SpawnerManager.NameToWeaponDict[weaponString];
        // spawner.weaponRespawnTimeInSeconds = _weaponRespawnTimeInSeconds;
    }
}
