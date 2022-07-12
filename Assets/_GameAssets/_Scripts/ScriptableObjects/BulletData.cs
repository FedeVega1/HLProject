using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "BulletData", menuName = "Data/Bullet")]
public class BulletData : ScriptableObject
{
    public GameObject bulletPrefab;
    public BulletType type;
    public DamageType damageType;
    public float initialSpeed, damage, maxTravelDistance, radius, timeToExplode, fallOff;
}
