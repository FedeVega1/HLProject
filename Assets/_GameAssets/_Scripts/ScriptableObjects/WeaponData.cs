using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Data/Weapon")]
public class WeaponData : ScriptableObject
{
    [System.Serializable]
    public struct WeaponAnimationTimings
    {
        public float initFire, fireMaxDelay, reload, holster, draw;
        public float zoomInSpeed, zoomOutSpeed, meleeHitboxIn, meleeHitboxOut;
    }
    
    public string weaponName;
    public GameObject clientPrefab, propPrefab;
    public BulletData bulletData;
    public float maxBulletSpread, weaponWeight, meleeDamage;
    public WeaponType weaponType;
    public WeaponAnimationTimings weaponAnimsTiming;
    public int bulletsPerMag, mags;
}
