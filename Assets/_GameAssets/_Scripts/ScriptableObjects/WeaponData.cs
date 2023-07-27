using UnityEngine;
using HLProject.Weapons;

namespace HLProject.Scriptables
{
    [CreateAssetMenu(fileName = "WeaponData", menuName = "Data/Weapon")]
    public class WeaponData : ScriptableObject
    {
        [System.Serializable]
        public struct WeaponAnimationTimings
        {
            public float initFire, fireMaxDelay, secondaryFireModeMaxDelay, thirdFireModeMaxDelay;
            public float reload, holster, draw, zoomInSpeed, zoomOutSpeed, meleeHitboxIn;
            public float meleeHitboxOut, shotgunPumpFireMaxDelay, initReload;
        }

        public string weaponName;
        public GameObject clientPrefab, propPrefab;
        public BulletData bulletData;
        public float maxBulletSpread, weaponWeight, meleeDamage;
        public DamageType meleeDamageType;
        public WeaponType weaponType;
        public WeaponAnimationTimings weaponAnimsTiming;
        public int bulletsPerMag, mags, pelletsPerShot = 1;
        public AnimationCurve recoilPatternX, recoilPatternY, recoilPatternZ;
        public bool singleRecoilShoot;
        public FireModes avaibleWeaponFireModes = FireModes.Single;
        public WeaponData alternateWeaponMode;
    }
}
