using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace HLProject
{
    public enum BulletType { RayCast, Physics }
    public enum WeaponType { Melee, Secondary, Primary, Tools, BandAids }

    public class Weapon : CachedTransform
    {
        public WeaponType WType => weaponData.weaponType;

        public Transform WeaponRootBone => clientWeapon.WeaponRootBone;
        public Transform ClientWeaponTransform => clientWeapon.MyTransform;

        double wTime, scopeTime;
        public int BulletsInMag { get; private set; }
        public int Mags { get; private set; }

        bool IsMelee => weaponData.weaponType == WeaponType.Melee;

        bool isReloading, onScope, weaponDrawn, firstFire, isFiring;
        RaycastHit rayHit;

        Player owningPlayer;
        BaseClientWeapon clientWeapon;
        LayerMask weaponLayerMask;
        WeaponData weaponData;
        BulletData bulletData;
        Transform firePivot;
        NetWeapon netWeapon;
        Collider[] meleeCollider;

        public System.Action OnFinishedReload;

        void Start()
        {
            weaponLayerMask = LayerMask.GetMask("PlayerHitBoxes", "SceneObjects");
            meleeCollider = new Collider[10];
        }

        public void Init(WeaponData data, NetWeapon serverSideWeapon, Player player)
        {
            owningPlayer = player;
            weaponData = data;
            bulletData = data.bulletData;
            netWeapon = serverSideWeapon;

            Mags = weaponData.mags;
            BulletsInMag = weaponData.bulletsPerMag;

            clientWeapon = Instantiate(weaponData.clientPrefab, MyTransform).GetComponent<BaseClientWeapon>();
            if (clientWeapon != null)
            {
                clientWeapon.Init(false, weaponData, bulletData, weaponData.propPrefab);
                clientWeapon.SetPlayerTeam(owningPlayer.GetPlayerTeam());
                firePivot = clientWeapon.GetVirtualPivot();
            }
            else
            {
                GameObject nullWGO = new GameObject($"{weaponData.weaponName} (NULL)");
                clientWeapon = nullWGO.AddComponent<NullWeapon>();
                clientWeapon.Init(false, weaponData, bulletData, weaponData.propPrefab);
                Debug.LogError("Client weapon prefab does not have a IWeapon type component.\nSwitching to default weapon");
            }
        }

        public void Fire()
        {
            if (NetworkTime.time < wTime || NetworkTime.time < scopeTime || isReloading || !weaponDrawn) return;

            if (IsMelee)
            {
                HandleMeleeSwing();
                return;
            }

            if (BulletsInMag <= 0)
            {
                clientWeapon.EmptyFire();
                wTime = NetworkTime.time + weaponData.weaponAnimsTiming.fireMaxDelay;
                return;
            }

            switch (bulletData.type)
            {
                case BulletType.RayCast:
                    HandleRaycast();
                    BulletsInMag--;
                    break;

                case BulletType.Physics:
                    clientWeapon.Fire(Vector3.zero, true, BulletsInMag);
                    BulletsInMag = 0;
                    Reload();
                    break;
            }

            if (weaponData.weaponName == "Shotgun" && owningPlayer.GetPlayerTeam() > 1)
                wTime = NetworkTime.time + weaponData.weaponAnimsTiming.shotgunPumpFireMaxDelay;
            else 
                wTime = NetworkTime.time + weaponData.weaponAnimsTiming.fireMaxDelay;

            //if (BulletsInMag == 0) ScopeOut();
            netWeapon.CmdRequestFire(firstFire);
            firstFire = false;
            isFiring = true;
        }

        void HandleRaycast()
        {
            for (int i = 0; i < weaponData.pelletsPerShot; i++)
            {
                Ray weaponRay = new Ray(firePivot.position, firePivot.forward + Random.onUnitSphere * Random.Range(.01f, weaponData.maxBulletSpread));
                if (Physics.Raycast(weaponRay, out rayHit, bulletData.maxTravelDistance, weaponLayerMask))
                {
                    Vector3 hitPos = rayHit.point;
                    bool fallOffCheck = CheckBulletFallOff(ref weaponRay, ref rayHit, out float distance);

                    if (!fallOffCheck) hitPos = rayHit.point;
                    clientWeapon.Fire(hitPos, true, BulletsInMag);

                    if (rayHit.collider != null) Debug.LogFormat("Client Fire! Range[{0}] - HitPoint: {1}|{2}", bulletData.maxTravelDistance, rayHit.point, rayHit.collider.name);
                }
                else
                {
                    Vector3 fallOff = Vector3.down * bulletData.fallOff;
                    clientWeapon.Fire(weaponRay.origin + ((weaponRay.direction + fallOff) * bulletData.maxTravelDistance), false, BulletsInMag);

                    Debug.DrawRay(weaponRay.origin, weaponRay.direction + fallOff, Color.red, 2);
                    Debug.DrawLine(weaponRay.origin, weaponRay.origin + ((weaponRay.direction + fallOff) * bulletData.maxTravelDistance), Color.red, 2);
                }
            }
        }

        void HandleMeleeSwing()
        {
            int quantity = Physics.OverlapBoxNonAlloc(firePivot.position, new Vector3(.5f, 1, .5f), meleeCollider, firePivot.rotation, weaponLayerMask);

            bool killHit = false, playerHit = false;
            for (int i = 0; i < quantity; i++)
            {
                HitBox hitBox = meleeCollider[i].GetComponent<HitBox>();
                if (hitBox == null) continue;
                if (hitBox.GetCharacterScript().MyTransform != owningPlayer.MyTransform)
                {
                    if (hitBox.GetCharacterScript().IsDead)
                    {
                        killHit = true;
                        break;
                    }

                    playerHit = true;
                    continue;
                }

                quantity--;
            }

            bool hit = quantity > 0;
            clientWeapon.MeleeSwing(hit, playerHit, killHit);
            netWeapon.CmdRequestFire(false);
            wTime = NetworkTime.time + weaponData.weaponAnimsTiming.fireMaxDelay;
        }

        bool CheckBulletFallOff(ref Ray weaponRay, ref RaycastHit rayHit, out float distance)
        {
            float fallStep = bulletData.maxTravelDistance / (float) NetWeapon.FallOffQuality;
            distance = bulletData.maxTravelDistance - fallStep;
            float lastDistance = distance;
            Vector3 lastHit = rayHit.point;

            for (int i = 0; i < NetWeapon.FallOffQuality; i++)
            {
                if (Physics.Raycast(weaponRay, out rayHit, distance, weaponLayerMask))
                {
                    Debug.LogFormat("Client Fire! Range[{0}] - HitPoint: {1}|{2}", distance, rayHit.point, rayHit.collider.name);
                    Debug.DrawLine(weaponRay.origin, rayHit.point, Color.yellow, 5);

                    lastDistance = distance;
                    distance -= fallStep;
                    weaponRay.direction += Vector3.down * bulletData.fallOff;
                    continue;
                }

                distance = lastDistance;
                rayHit.point = lastHit;
                return false;
            }

            return true;
        }

        public void InitFire()
        {
            firstFire = true;
        }

        public void EndFire()
        {
            if (!isFiring) return;
            netWeapon.CmdFireRelease();
            isFiring = false;
        }

        public void AltFire()
        {
            clientWeapon.AltFire(Vector3.zero, false);
        }

        public void ScopeIn()
        {
            if (isReloading || onScope) return;
            clientWeapon.ScopeIn();
            netWeapon.CmdRequestScopeIn();
            onScope = true;
            scopeTime = NetworkTime.time + weaponData.weaponAnimsTiming.zoomInSpeed;
        }

        public void ScopeOut()
        {
            if (!onScope) return;
            clientWeapon.ScopeOut();
            netWeapon.CmdRequestScopeOut();
            onScope = false;
            scopeTime = NetworkTime.time + weaponData.weaponAnimsTiming.zoomOutSpeed;
        }

        public void Reload()
        {
            if (isReloading || onScope || BulletsInMag >= weaponData.bulletsPerMag || Mags <= 0) return;
            isReloading = true;
            int diff = weaponData.bulletsPerMag - BulletsInMag;
            clientWeapon.Reload(diff);
            StartCoroutine(ReloadRoutine(weaponData.weaponName == "Shotgun" ? diff : 1));
            netWeapon.CmdRequestReload();
        }

        IEnumerator ReloadRoutine(int bullets)
        {
            yield return new WaitForSeconds(weaponData.weaponAnimsTiming.reload + bullets);
            isReloading = false;
            BulletsInMag = weaponData.bulletsPerMag;
            Mags -= bullets;
            OnFinishedReload?.Invoke();
        }

        public double ToggleWeapon(bool toggle)
        {
            weaponDrawn = toggle;

            if (toggle)
            {
                clientWeapon.DrawWeapon();
                wTime = NetworkTime.time + weaponData.weaponAnimsTiming.draw;
                return weaponData.weaponAnimsTiming.draw;
            }

            clientWeapon.HolsterWeapon();
            wTime = NetworkTime.time + weaponData.weaponAnimsTiming.holster;
            return weaponData.weaponAnimsTiming.holster;
        }

        public void ToggleWeaponSway(bool toggle) => clientWeapon.ToggleWeaponSway(toggle);

        public void DropWeapon() => clientWeapon.DropProp();

        public WeaponData GetWeaponData() => weaponData;

        public void CheckPlayerMovement(bool isMoving, bool isRunning) => clientWeapon.CheckPlayerMovement(isMoving, isRunning);
    }
}
