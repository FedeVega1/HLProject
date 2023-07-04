using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HLProject
{
    public class Grenade : BaseClientWeapon
    {
        [SerializeField] Animator weaponAnim;

        public override void Fire(Vector3 destination, bool didHit, int ammo)
        {

        }

        public override void EmptyFire() { }


        public override void AltFire(Vector3 destination, bool didHit)
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

        public override Transform GetVirtualPivot() => virtualBulletPivot;
        public override Transform GetWorldPivot() => worldBulletPivot;

        public override void CheckPlayerMovement(bool isMoving, bool isRunning) { }
    }
}
