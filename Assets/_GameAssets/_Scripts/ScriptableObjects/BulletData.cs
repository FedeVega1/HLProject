using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HLProject.Weapons;

namespace HLProject.Scriptables
{
    [System.Serializable]
    [CreateAssetMenu(fileName = "BulletData", menuName = "Data/Bullet")]
    public class BulletData : ScriptableObject
    {
        public GameObject bulletPrefab;
        public BulletType type;
        public BulletPhysicsType physType;
        public DamageType damageType;
        public bool canExplode;
        public int bouncesToExplode;
        public float initialSpeed, damage, maxTravelDistance, radius, timeToExplode, fallOff, explosionDamage;
    }
}
