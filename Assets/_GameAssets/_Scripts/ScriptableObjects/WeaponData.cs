using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Data/Weapon")]
public class WeaponData : ScriptableObject
{
    public string weaponName;
    public GameObject clientPrefab, propPrefab;
    public BulletData bulletData;
    public float maxBulletSpread, rateOfFire;
    public WeaponType weaponType;
}
