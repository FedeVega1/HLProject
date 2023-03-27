using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Data/Weapon")]
public class WeaponData : ScriptableObject
{
    [System.Serializable]
    public struct WeaponAnimationTimings
    {
        public float fire, reload;
        public float holster, draw;
    }

    public string weaponName;
    public GameObject clientPrefab, propPrefab;
    public BulletData bulletData;
    public float maxBulletSpread;
    public WeaponType weaponType;
    public WeaponAnimationTimings weaponAnimsTiming;
    public int bulletsPerMag, mags;
}
