using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BulletData", menuName = "Data/Bullet")]
public class BulletData : ScriptableObject
{
    public GameObject bulletPrefab;
    public BulletType type;
    public float initialSpeed, damage, mass, maxTravelDistance;
}
