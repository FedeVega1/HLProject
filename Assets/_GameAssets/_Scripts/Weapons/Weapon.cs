using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using HLProject.Scriptables;

namespace HLProject.Weapons
{
    public enum BulletType { RayCast, Physics }
    public enum WeaponType { Melee, Secondary, Primary, AltMode, Tools, BandAids }
    [System.Flags] public enum FireModes { Single = 0b1, Auto = 0b10, Burst = 0b100 }

    public class Weapon : CachedTransform
    {
        public WeaponType WType => weaponData.weaponType;
        public WeaponType LastWType => swappedData.weaponType;
        public bool OnAltMode => swappedData != null;

        public Transform WeaponRootBone => clientWeapon.WeaponRootBone;
        public Transform ClientWeaponTransform => clientWeapon.MyTransform;

        double wTime, scopeTime;
        public int BulletsInMag { get; private set; }
        public int Mags { get; private set; }

        bool IsMelee => weaponData.weaponType == WeaponType.Melee;

        bool isReloading, onScope, weaponDrawn, firstFire, isFiring;
        RaycastHit rayHit;
        int fireModeCycler, swapBullets = -1, swapMags = -1;
        int minValue, maxValue;

        Player owningPlayer;
        BaseClientWeapon clientWeapon;
        LayerMask weaponLayerMask;
        FireModes currentWeaponFireMode;
        WeaponData weaponData, swappedData;
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
            SetupFireMode();

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

        void SetupFireMode()
        {
            if ((weaponData.avaibleWeaponFireModes & FireModes.Single) == FireModes.Single)
            {
                currentWeaponFireMode = FireModes.Single;
                minValue = 0;
            }
            else if ((weaponData.avaibleWeaponFireModes & FireModes.Burst) == FireModes.Burst)
            {
                currentWeaponFireMode = FireModes.Burst;
                minValue = 2;
            }
            else
            { 
                currentWeaponFireMode = FireModes.Auto;
                minValue = 1;
            }

            if ((weaponData.avaibleWeaponFireModes & FireModes.Auto) == FireModes.Auto)
                maxValue = 1;
            else if ((weaponData.avaibleWeaponFireModes & FireModes.Burst) == FireModes.Burst)
                maxValue = 2;
            else
                maxValue = 0;
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
                    Handlephysics();
                    BulletsInMag = 0;
                    break;
            }

            if (weaponData.weaponName == "Shotgun" && owningPlayer.GetPlayerTeam() > 1)
                wTime = NetworkTime.time + weaponData.weaponAnimsTiming.shotgunPumpFireMaxDelay;
            else
                wTime = NetworkTime.time + currentWeaponFireMode switch
                {
                    FireModes.Single => weaponData.weaponAnimsTiming.fireMaxDelay,
                    FireModes.Auto => weaponData.weaponAnimsTiming.secondaryFireModeMaxDelay,
                    _ => weaponData.weaponAnimsTiming.thirdFireModeMaxDelay
                };

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
                    if (OnAltMode) clientWeapon.AltFire(hitPos, true, BulletsInMag);
                    else clientWeapon.Fire(hitPos, true, BulletsInMag);

                    if (rayHit.collider != null) Debug.LogFormat("Client Fire! Range[{0}] - HitPoint: {1}|{2}", bulletData.maxTravelDistance, rayHit.point, rayHit.collider.name);
                }
                else
                {
                    Vector3 fallOff = Vector3.down * bulletData.fallOff;
                    Vector3 dest = weaponRay.origin + ((weaponRay.direction + fallOff) * bulletData.maxTravelDistance);

                    if (OnAltMode) clientWeapon.AltFire(dest, false, BulletsInMag);
                    else clientWeapon.Fire(dest, false, BulletsInMag);

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

        void Handlephysics()
        {
            if (OnAltMode) clientWeapon.AltFire(Vector3.zero, true, BulletsInMag);
            else clientWeapon.Fire(Vector3.zero, true, BulletsInMag);

            switch (bulletData.physType)
            {
                case BulletPhysicsType.Throw:
                    Reload();
                    break;

                case BulletPhysicsType.Fire:
                    break;
            }
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

        public double ToggleAltMode()
        {
            int bullets = swapBullets;
            int mags = swapMags;

            if (OnAltMode)
            {
                swapBullets = BulletsInMag;
                swapMags = Mags;

                BulletsInMag = bullets;
                Mags = mags;

                weaponData = swappedData;
                swappedData = null;
                bulletData = weaponData.bulletData;
                clientWeapon.OnAltMode(false, weaponData);
                return .1f;
            }

            if (weaponData.alternateWeaponMode == null) return .1f;

            swapBullets = BulletsInMag;
            swapMags = Mags;

            swappedData = weaponData;
            weaponData = weaponData.alternateWeaponMode;

            BulletsInMag = bullets == -1 ? weaponData.bulletsPerMag : bullets;
            Mags = mags == -1 ? weaponData.mags : mags;

            bulletData = weaponData.bulletData;
            clientWeapon.OnAltMode(true, weaponData);
            return .1f;
        }

        public void ScopeIn()
        {
            if (isReloading || onScope) return;
            clientWeapon.ScopeIn();
            netWeapon.CmdRequestScopeIn();
            onScope = true;
            scopeTime = NetworkTime.time + weaponData.weaponAnimsTiming.zoomInSpeed;
        }

        public void SwitchFireMode()
        {
            if (minValue == maxValue) return;
            fireModeCycler++;
            if (fireModeCycler > maxValue) fireModeCycler = minValue;
            if (fireModeCycler < minValue) fireModeCycler = maxValue;

            clientWeapon.ChangeFireMode((int) fireModeCycler);

            currentWeaponFireMode = fireModeCycler switch
            {
                0 => FireModes.Single,
                1 => FireModes.Auto,
                _ => FireModes.Burst,
            };

            netWeapon.CmdRequestChangeFireMode((int) currentWeaponFireMode);
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

        public void ToggleWeaponVisibility(bool toggle)
        {
            clientWeapon.gameObject.SetActive(toggle);
        }

        public double ToggleWeapon(bool toggle)
        {
            weaponDrawn = toggle;

            if (toggle)
            {
                if (OnAltMode) ToggleAltMode();
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
