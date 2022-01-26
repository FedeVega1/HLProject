using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "WeaponData", menuName = "Data/Weapon")]
public class WeaponData : ScriptableObject
{
    public string weaponName;
    public GameObject clientPrefab;
    public BulletData bulletData;
    public float maxBulletSpread, rateOfFire;
}
