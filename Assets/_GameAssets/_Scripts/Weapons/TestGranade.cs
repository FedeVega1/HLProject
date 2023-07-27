using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HLProject.Scriptables;

namespace HLProject.Weapons
{
    public class TestGranade : BaseClientWeapon
    {
        [SerializeField] Animator weaponAnim;

        public override void Fire(Vector3 destination, bool didHit, int ammo)
        {
            //if (!isDrawn) return;
            //GameObject bulletObject = Instantiate(bulletData.bulletPrefab, isServer ? worldBulletPivot : virtualBulletPivot);
            //Bullet bullet = bulletObject.GetComponent<Bullet>();

            //bullet.Init(bulletData.initialSpeed, didHit);
            //bullet.TravelTo(destination);
            //bullet.MyTransform.parent = null;
        }

        public override void EmptyFire() { }


        public override void AltFire(Vector3 destination, bool didHit, int ammo)
        {
            if (!isDrawn) return;

        }

        public override void ScopeIn()
        {
            if (!isDrawn) return;

        }

        public override void ScopeOut()
        {
            if (!isDrawn) return;

        }

        public override void Reload(int bulletsToReload) { }

        public override void DrawWeapon()
        {
            gameObject.SetActive(true);
            isDrawn = true;
        }

        public override void HolsterWeapon()
        {
            gameObject.SetActive(false);
            isDrawn = false;
        }

        public override void DropProp()
        {
            Instantiate(weaponPropPrefab, MyTransform.position, MyTransform.rotation);
            Destroy(gameObject);
        }

        public override void OnAltMode(bool toggle, WeaponData newData) { }

        public override Transform GetVirtualPivot() => virtualBulletPivot;
        public override Transform GetWorldPivot() => worldBulletPivot;

        public override void CheckPlayerMovement(bool isMoving, bool isRunning) { }
    }
}
