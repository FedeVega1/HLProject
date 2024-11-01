using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using HLProject.Scriptables;
using HLProject.Characters;

namespace HLProject.Weapons
{
    public enum BulletPhysicsType { Throw, Fire, FireBounce }

    public class NetWeapon : CachedNetTransform
    {
        public const int FallOffQuality = 5;

        public WeaponType WType => weaponData.weaponType;
        public WeaponType LastWType => swappedData != null ? swappedData.weaponType : weaponData.weaponType;

        [SyncVar] bool serverInitialized;

        public bool IsDroppingWeapons { get; private set; }
        public double TimeForNextShoot => fireTime;
        public int WeaponAmmo => bulletsInMag;
        public int WeaponMags => mags;
        public bool IsReloading => isReloading;

        bool IsMelee => weaponData.weaponType == WeaponType.Melee;

        bool clientWeaponInit, isReloading, onScope;
        int bulletsInMag, mags, swapBullets = -1, swapMags = -1;
        float recoilMult = 1;
        double fireTime, scopeTime, holdingFireStartTime;
        RaycastHit rayHit;

        WeaponData swappedData;
        WeaponData weaponData;
        FireModes currentFireMode;

        Player owningPlayer;
        BaseClientWeapon clientWeapon;
        LayerMask weaponLayerMask;
        BulletData bulletData;
        Transform firePivot;
        Collider[] raycastShootlastCollider;
        Coroutine meleeRoutine;
        Queue<BulletData> lastShootPhysExplosion;

        AsyncOperationHandle<WeaponData> clientSyncWeaponData;
        AsyncOperationHandle<IList<BulletData>> clientBulletData;

        public override void OnStartClient()
        {
            if (hasAuthority) return;
            CmdRequestWeaponData();
        }

        void Start() => weaponLayerMask = LayerMask.GetMask("PlayerHitBoxes", "SceneObjects");

        public void Init(WeaponData data, Player owner)
        {
            weaponData = data;
            RpcSyncWeaponData(weaponData.name);

            lastShootPhysExplosion = new Queue<BulletData>();
            bulletData = data.bulletData;
            owningPlayer = owner;

            raycastShootlastCollider = new Collider[10];

            mags = weaponData.mags;
            bulletsInMag = weaponData.bulletsPerMag;

            CheckAvaibleFireModes();

            GameObject weaponObj = Instantiate(weaponData.clientPrefab, MyTransform);
            BaseClientWeapon cWeapon = weaponObj.GetComponent<BaseClientWeapon>();
            if (cWeapon != null)
            {
                cWeapon.ToggleAllViewModels(false);
                firePivot = cWeapon.GetVirtualPivot();
                firePivot.SetParent(weaponObj.transform);
            }
            else
            {
                firePivot = MyTransform;
            }

            serverInitialized = true;
            RpcInitClientWeapon();
        }

        void CheckAvaibleFireModes()
        {
            if ((weaponData.avaibleWeaponFireModes & FireModes.Single) == FireModes.Single)
                currentFireMode = FireModes.Single;
            else if ((weaponData.avaibleWeaponFireModes & FireModes.Burst) == FireModes.Burst)
                currentFireMode = FireModes.Burst;
            else
                currentFireMode = FireModes.Auto;
        }

        #region Server

        [Server]
        void Fire()
        {
            if (owningPlayer.PlayerIsRunning() || NetworkTime.time < fireTime || NetworkTime.time < scopeTime || isReloading) return;

            if (IsMelee)
            {
                meleeRoutine = StartCoroutine(MeleeSwing());
                owningPlayer.AnimController.OnPlayerShoots(swappedData != null);
                return;
            }

            if (bulletsInMag <= 0) return;

            //MyTransform.eulerAngles = new Vector3(owningPlayer.GetPlayerCameraXAxis(), MyTransform.eulerAngles.y, MyTransform.eulerAngles.z);

            switch (bulletData.type)
            {
                case BulletType.RayCast:
                    ShootRayCastBullet();
                    break;

                case BulletType.Physics:
                    LeanTween.delayedCall(weaponData.weaponAnimsTiming.initFire, ShootPhysicsBullet);
                    break;
            }

            owningPlayer.AnimController.OnPlayerShoots(swappedData != null);

            LeanTween.delayedCall(weaponData.weaponAnimsTiming.initFire, () =>
            {
                double time = NetworkTime.time - holdingFireStartTime;
                owningPlayer.ApplyRecoil(new Vector3(weaponData.recoilPatternX.Evaluate((float) time), weaponData.recoilPatternY.Evaluate((float) time), weaponData.recoilPatternZ.Evaluate((float) time)) * recoilMult);
            });

            if (currentFireMode != FireModes.Auto)
                LeanTween.delayedCall(weaponData.weaponAnimsTiming.initFire + .1f, () => owningPlayer.ResetRecoil());

            if (weaponData.weaponName == "Shotgun" && owningPlayer.GetPlayerTeam() > 1)
                fireTime = NetworkTime.time + weaponData.weaponAnimsTiming.shotgunPumpFireMaxDelay;
            else
                fireTime = NetworkTime.time + (currentFireMode switch { 
                    FireModes.Single => weaponData.weaponAnimsTiming.fireMaxDelay,
                    FireModes.Auto => weaponData.weaponAnimsTiming.secondaryFireModeMaxDelay, 
                    _ => weaponData.weaponAnimsTiming.thirdFireModeMaxDelay
                });
        }

        [Server]
        void ShootPhysicsBullet()
        {
            GameObject granade = Instantiate(bulletData.bulletPrefab, firePivot.position + firePivot.forward, firePivot.rotation);
            Bullet granadeBulletScript = granade.GetComponent<Bullet>();
            granadeBulletScript.OnExplode += OnBulletExplode;
            granadeBulletScript.OnTouch += OnBulletTouch;
            granadeBulletScript.Init(bulletData.initialSpeed, true);

            Vector3 cross;
            switch (bulletData.physType)
            {
                case BulletPhysicsType.Throw:
                    cross = new Vector3(0, .1f, .9f);
                    granadeBulletScript.PhysicsTravelTo(true, MyTransform.TransformDirection(cross), ForceMode.Force, true, true, bulletData.radius, false, bulletData.timeToExplode);
                    bulletsInMag--;
                    Reload(true);
                    break;

                case BulletPhysicsType.Fire:
                    cross = new Vector3(0, .2f, .8f);
                    granadeBulletScript.PhysicsTravelTo(true, MyTransform.TransformDirection(cross), ForceMode.Impulse, false, true, bulletData.radius, true);
                    break;

                case BulletPhysicsType.FireBounce:
                    granadeBulletScript.PhysicsTravelTo(true, firePivot.forward, ForceMode.Impulse, false, true, bulletData.radius, false, bulletData.timeToExplode, bulletData.bouncesToExplode);
                    break;
            }

            if (bulletsInMag > 0) bulletsInMag--;
            lastShootPhysExplosion.Enqueue(bulletData);
            NetworkServer.Spawn(granade);
        }

        [Server]
        void OnBulletExplode(List<HitBox> hitBoxList, Vector3 finalPos, Quaternion finalRot)
        {
            if (bulletData.explosionDamage != 0)
            {
                int size = hitBoxList.Count;
                for (int i = 0; i < size; i++)
                {
                    float distance = Vector3.Distance(finalPos, hitBoxList[i].MyTransform.position);
                    float damageFalloff = Mathf.Clamp(bulletData.radius - distance, 0, bulletData.radius) / bulletData.radius;
                    Vector3 direction = Vector3.Normalize(hitBoxList[i].MyTransform.position - finalPos);
                    hitBoxList[i].TakeDamage(bulletData.explosionDamage * damageFalloff, bulletData.damageType, direction, finalPos, bulletData.radius);
                }
            }

            RpcBulletExplosion(finalPos, finalRot, lastShootPhysExplosion.Dequeue().name);
        }

        [Server]
        void OnBulletTouch(HitBox target, Vector3 dir)
        {
            target.TakeDamage(bulletData.damage, bulletData.damageType, dir);
        }

        [Server]
        void ShootRayCastBullet()
        {
            Ray weaponRay;
            bool[] didHit = new bool[weaponData.pelletsPerShot];
            Vector3[] hitVectors = new Vector3[weaponData.pelletsPerShot];

            for (int i = 0; i < weaponData.pelletsPerShot; i++)
            {
                weaponRay = new Ray(firePivot.position, firePivot.forward + Random.onUnitSphere * Random.Range(.01f, weaponData.maxBulletSpread));
                if (Physics.Raycast(weaponRay, out rayHit, bulletData.maxTravelDistance, weaponLayerMask) && rayHit.transform.root != owningPlayer.MyTransform)
                {
                    DoBulletFlyby(weaponRay, rayHit.distance);
                    hitVectors[i] = rayHit.point;

                    bool fallOffCheck = CheckBulletFallOff(ref weaponRay, ref rayHit, out float distance);
                    if (!fallOffCheck) hitVectors[i] = rayHit.point;
                    didHit[i] = true;

                    if (fallOffCheck)
                    {
                        StartCoroutine(ApplyDistanceToDamage(hitVectors[i], rayHit.distance, weaponRay.direction));
                        Debug.DrawLine(weaponRay.origin, hitVectors[i], Color.green, 5);

                        //HitBox hitBox = rayHit.transform.GetComponent<HitBox>();
                        //if (hitBox != null)
                        //{
                        //    StartCoroutine(ApplyDistanceToDamage(hitBox, rayHit.distance));
                        //    Debug.DrawLine(weaponRay.origin, hitPos, Color.green, 5);
                        //    return;
                        //}
                    }

                    if (rayHit.collider != null) Debug.LogFormat("Server Fire! Range[{0}] - HitPoint: {1}|{2}", bulletData.maxTravelDistance, rayHit.point, rayHit.collider.name);
                    //else RpcFireWeapon(weaponRay.origin + (weaponRay.direction * distance), false);
                }

                DoBulletFlyby(weaponRay, bulletData.maxTravelDistance);
                Vector3 fallOff = Vector3.down * bulletData.fallOff;
                hitVectors[i] = weaponRay.origin + ((weaponRay.direction + fallOff) * bulletData.maxTravelDistance);
                didHit[i] = false;

                Debug.DrawRay(weaponRay.origin, weaponRay.direction + fallOff, Color.red, 2);
                Debug.DrawLine(weaponRay.origin, weaponRay.origin + ((weaponRay.direction + fallOff) * bulletData.maxTravelDistance), Color.red, 2);
            }

            //if (bulletsInMag == 0) ScopeOut();
            bulletsInMag--;
            RpcFireWeapon(hitVectors, didHit);
        }

        bool CheckBulletFallOff(ref Ray weaponRay, ref RaycastHit rayHit, out float distance)
        {
            float fallStep = bulletData.maxTravelDistance / (float) FallOffQuality;
            distance = bulletData.maxTravelDistance - fallStep;
            float lastDistance = distance;
            Vector3 lastHit = rayHit.point;

            for (int i = 0; i < FallOffQuality; i++)
            {
                if (Physics.Raycast(weaponRay, out rayHit, distance, weaponLayerMask))
                {
                    Debug.LogFormat("Server Fire! Range[{0}] - HitPoint: {1}|{2}", distance, rayHit.point, rayHit.collider.name);
                    Debug.DrawLine(weaponRay.origin, rayHit.point, Color.yellow, 5);

                    lastDistance = distance;
                    //distance -= fallStep;
                    weaponRay.direction += Vector3.down * bulletData.fallOff;
                    continue;
                }

                distance = lastDistance;
                rayHit.point = lastHit;
                return false;
            }

            return true;
        }

        [Server]
        void DoBulletFlyby(Ray ray, float distance)
        {
            RaycastHit[] hits = Physics.SphereCastAll(ray, .2f, distance, weaponLayerMask);

            int size = hits.Length;
            for (int i = 0; i < size; i++)
            {
                HitBox boxTarget = hits[i].transform.GetComponent<HitBox>();
                if (boxTarget == null || boxTarget.CharacterTransform == owningPlayer.MyTransform) continue;
                boxTarget.OnBulletFlyby(hits[i].point);
            }
        }

        [Server]
        public void ToggleAltMode()
        {
            int bullets = swapBullets;
            int _mags = swapMags;

            if (swappedData != null)
            {
                swapBullets = bulletsInMag;
                swapMags = mags;

                bulletsInMag = bullets;
                mags = _mags;

                weaponData = swappedData;
                bulletData = weaponData.bulletData;
                CheckAvaibleFireModes();
                swappedData = null;
                return;
            }

            if (weaponData.alternateWeaponMode == null) return;

            swapBullets = bulletsInMag;
            swapMags = mags;

            swappedData = weaponData;
            weaponData = weaponData.alternateWeaponMode;

            bulletsInMag = bullets == -1? weaponData.bulletsPerMag : bullets;
            mags = _mags == -1 ? weaponData.mags : _mags;

            bulletData = weaponData.bulletData;
            CheckAvaibleFireModes();
        }

        [Server]
        void Reload(bool forceReload)
        {
            if (IsMelee || isReloading || onScope || bulletsInMag >= weaponData.bulletsPerMag || mags <= 0)
            {
                Debug.LogFormat("Reload order failed: {0} - {1} - {2} - {3} - {4}", IsMelee, isReloading, onScope, bulletsInMag >= weaponData.bulletsPerMag, mags <= 0);
                return;
            }

            owningPlayer.TogglePlayerRunAbility(false);
            isReloading = true;
            RpcReloadWeapon(bulletsInMag - weaponData.bulletsPerMag, forceReload);
            owningPlayer.AnimController.OnPlayerReloads();
            StartCoroutine(ReloadRoutine(weaponData.weaponName == "Shotgun" ? (weaponData.bulletsPerMag - bulletsInMag) : 1));
        }

        [Server]
        public void DropClientWeaponAndDestroy()
        {
            IsDroppingWeapons = true;
            RpcDropWeapon();
        }

        [Server]
        public void ForceFire(bool firstFire)
        {
            if (firstFire) holdingFireStartTime = NetworkTime.time;
            Fire();
        }

        [Server] public void ForceReleaseFire() => owningPlayer.ResetRecoil();
        [Server] public void ForceScopeIn() => ScopeIn();
        [Server] public void ForceScopeOut() => ScopeOut();
        [Server] public void ForceReload() => Reload(false);

        [Server]
        public void HideWeapons()
        {
            clientWeapon.ReleaseStaticSoundsHandles();
            RpcToggleClientWeapon(false);
        }

        [Server]
        void ScopeIn()
        {
            if (IsMelee || owningPlayer.PlayerIsRunning() || isReloading || onScope) return;
            recoilMult = .5f;
            owningPlayer.TogglePlayerScopeStatus(true);
            onScope = true;
            scopeTime = NetworkTime.time + weaponData.weaponAnimsTiming.zoomInSpeed;
        }

        [Server]
        void ScopeOut()
        {
            if (IsMelee || !onScope) return;
            owningPlayer.TogglePlayerScopeStatus(false);
            recoilMult = 1;
            onScope = false;
            scopeTime = NetworkTime.time + weaponData.weaponAnimsTiming.zoomOutSpeed;
        }

        [Server]
        public void OnWeaponSwitch()
        {
            if (meleeRoutine != null)
            {
                StopCoroutine(meleeRoutine);
                meleeRoutine = null;
            }

            if (swappedData != null) ToggleAltMode();
        }

        #endregion

        #region Commands

        [Command]
        public void CmdRequestChangeFireMode(int newFireMode)
        {
            currentFireMode = (FireModes) newFireMode;
        }

        [Command]
        public void CmdRequestFire(bool firstFire)
        {
            if (firstFire) holdingFireStartTime = NetworkTime.time;
            Fire();
        }

        [Command]
        public void CmdFireRelease()
        {
            owningPlayer.ResetRecoil();
        }

        [Command]
        public void CmdRequestReload()
        {
            Reload(false);
        }

        [Command(requiresAuthority = false)]
        public void CmdRequestWeaponData()
        {
            RpcSyncWeaponData(weaponData.name);
        }

        [Command(requiresAuthority = false)]
        public void CmdOnPlayerDroppedWeapon()
        {
            if (!IsDroppingWeapons) return;
            NetworkServer.Destroy(gameObject);
        }

        [Command]
        public void CmdRequestScopeIn() => ScopeIn();

        [Command]
        public void CmdRequestScopeOut() => ScopeOut();

        #endregion

        #region RPCs

        [ClientRpc]
        public void RpcBulletExplosion(Vector3 pos, Quaternion rot, string bulletDataName)
        {
            int indx = SearchForBulletData(bulletDataName);
            if (indx == -1)
            {
                Debug.LogErrorFormat("Couldn't find the requested bullet data: {0}", bulletDataName);
                return;
            }

            GameObject granade = Instantiate(clientBulletData.Result[indx].bulletPrefab, pos, rot);
            Bullet granadeBulletScript = granade.GetComponent<Bullet>();
            granadeBulletScript.Init(0, true);
            granadeBulletScript.PhysicsTravelTo(false, Vector3.zero, ForceMode.Force, false, true, bulletData.radius, true, Mathf.Epsilon);
        }

        [ClientRpc(includeOwner = false)]
        public void RpcToggleClientWeapon(bool toggle)
        {
            StartCoroutine(WaitForServerAndClientInitialization(true, true, () => {
                if (toggle) clientWeapon.DrawWeapon();
                else clientWeapon.HolsterWeapon();
            }));
        }

        [ClientRpc]
        public void RpcInitClientWeapon()
        {
            clientBulletData = Addressables.LoadAssetsAsync<BulletData>(new List<string> { "WeaponBulletScriptables" }, null, Addressables.MergeMode.Union);
            clientBulletData.Completed += OnLoadBulletData;

            if (hasAuthority) return;
            print("INIT CLIENT WEAPON");
            InitClientWeapon();
        }

        [ClientRpc(includeOwner = false)]
        public void RpcFireWeapon(Vector3[] destinations, bool[] didHit)
        {
            int size = destinations.Length;
            for (int i = 0; i < size; i++)
                clientWeapon.Fire(destinations[i], didHit[i], -1);
        }

        [ClientRpc]
        public void RpcReloadWeapon(int bullets, bool forceOwnerReload)
        {
            if (hasAuthority)
            {
                if (forceOwnerReload) 
                    owningPlayer.ForceClientReload();
                return;
            }
            clientWeapon.Reload(bullets);
        }

        [ClientRpc]
        void RpcSyncWeaponData(string weaponAssetName)//(int weaponID)
        {
            print("Client Sync!");
            if (weaponData != null) return;
            clientSyncWeaponData = Addressables.LoadAssetAsync<WeaponData>(weaponAssetName + ".asset");
            clientSyncWeaponData.Completed += OnClientSyncWeaponLoaded;
        }

        void OnClientSyncWeaponLoaded(AsyncOperationHandle<WeaponData> operation)
        {
            if (operation.Status == AsyncOperationStatus.Failed)
            {
                Debug.LogErrorFormat("Couldn't load the requested weapon: {0}", operation.OperationException);
                return;
            }

            //if (weaponData == null) weaponData = Resources.Load<WeaponData>($"Weapons/{weaponAssetName}");
            weaponData = clientSyncWeaponData.Result;
            if (bulletData == null) bulletData = weaponData.bulletData;
            if (serverInitialized && clientWeapon == null) InitClientWeapon();
        }

        void OnLoadBulletData(AsyncOperationHandle<IList<BulletData>> operation)
        {
            if (operation.Status == AsyncOperationStatus.Failed)
                Debug.LogErrorFormat("Couldn't load bullet data: {0}", operation.OperationException);
        }

        [ClientRpc]
        public void RpcDropWeapon()
        {
            if (hasAuthority)
            {
                CmdOnPlayerDroppedWeapon();
                return;
            }

            StartCoroutine(WaitForServerAndClientInitialization(true, true, () => {
                clientWeapon.DropProp();
                CmdOnPlayerDroppedWeapon();
            }));
        }

        #endregion

        #region Coroutines

        [Server]
        IEnumerator MeleeSwing()
        {
            yield return new WaitForSeconds(weaponData.weaponAnimsTiming.meleeHitboxIn);

            float hitboxTime = 0;
            while (hitboxTime < weaponData.weaponAnimsTiming.meleeHitboxOut)
            {
                int quantity = Physics.OverlapBoxNonAlloc(firePivot.position, new Vector3(.5f, 1, .5f), raycastShootlastCollider, firePivot.rotation, weaponLayerMask);
                /*GameObject test = GameObject.CreatePrimitive(PrimitiveType.Cube);
                test.GetComponent<Collider>().enabled = false;
                test.transform.SetPositionAndRotation(firePivot.position, firePivot.rotation);
                test.transform.localScale = new Vector3(.5f, 1, .5f);*/

                for (int i = 0; i < quantity; i++)
                {
                    HitBox hitBoxToHit = raycastShootlastCollider[i].GetComponent<HitBox>();
                    if (hitBoxToHit == null || hitBoxToHit.CharacterTransform == owningPlayer.MyTransform) continue;

                    hitBoxToHit.TakeDamage(weaponData.meleeDamage, DamageType.Base, Vector3.Normalize(hitBoxToHit.CharacterTransform.position - firePivot.position));
                    meleeRoutine = null;
                    yield break;
                }

                if (quantity > 0)
                {
                    meleeRoutine = null;
                    yield break;
                }

                hitboxTime += Time.deltaTime;
                yield return null;
            }

            meleeRoutine = null;
        }

        [Server]
        IEnumerator ApplyDistanceToDamage(Vector3 hitOrigin, float distance, Vector3 hitDirection)
        {
            yield return new WaitForSeconds((distance / bulletData.initialSpeed) + weaponData.weaponAnimsTiming.initFire);
            int quantity = Physics.OverlapSphereNonAlloc(hitOrigin, .1f, raycastShootlastCollider, weaponLayerMask);

            for (int i = 0; i < quantity; i++)
            {
                HitBox hitBoxToHit = raycastShootlastCollider[i].GetComponent<HitBox>();
                if (hitBoxToHit == null || hitBoxToHit.CharacterTransform == owningPlayer.MyTransform) continue;

                hitBoxToHit.TakeDamage(bulletData.damage, DamageType.Bullet, hitDirection);
                yield break;
            }
        }

        [Server]
        IEnumerator ReloadRoutine(int bulletsToReload)
        {
            yield return new WaitForSeconds(weaponData.weaponAnimsTiming.reload * bulletsToReload);
            bulletsInMag = weaponData.bulletsPerMag;
            mags -= bulletsToReload;
            isReloading = false;
            owningPlayer.TogglePlayerRunAbility(true);
            print("Server finish Reload!");
        }

        [Client]
        IEnumerator WaitForServerAndClientInitialization(bool waitForClient, bool waitForServer, System.Action OnServerAndClientInitialized)
        {
            int debug = 0;
            while ((!serverInitialized && waitForServer) || (!clientWeaponInit && waitForClient))
            {
                Debug.LogFormat("Waiting for client Initialization {0}", debug++);
                yield return new WaitForSeconds(.1f);
            }
            OnServerAndClientInitialized?.Invoke();
        }

        #endregion

        [Client]
        void InitClientWeapon()
        {
            if (hasAuthority) return;
            StartCoroutine(WaitForServerAndClientInitialization(false, true, () =>
            {
                clientWeapon = Instantiate(weaponData.clientPrefab, MyTransform).GetComponent<BaseClientWeapon>();
                if (clientWeapon != null)
                {
                    clientWeapon.Init(true, weaponData, bulletData, weaponData.propPrefab);
                }
                else
                {
                    GameObject nullWGO = new GameObject($"{weaponData.weaponName} (NULL)");
                    clientWeapon = nullWGO.AddComponent<NullWeapon>();
                    clientWeapon.Init(true, weaponData, bulletData, weaponData.propPrefab);
                    Debug.LogError("Client weapon prefab does not have a IWeapon type component.\nSwitching to default weapon");
                }

                clientWeaponInit = true;
            }));
        }

        public WeaponData GetWeaponData() => weaponData;

        [Client]
        int SearchForBulletData(string dataName)
        {
            if (!clientBulletData.IsValid())
            {
                Debug.LogError("Client Bullet data List is not valid!");
                return -1;
            }

            if (clientBulletData.Result == null)
            {
                Debug.LogError("Client Bullet data List is null!");
                return -1;
            }

            int size = clientBulletData.Result.Count;
            for (int i = 0; i < size; i++)
            {
                if (clientBulletData.Result[i].name == dataName)
                    return i;
            }

            return -1;
        }
    }
}
