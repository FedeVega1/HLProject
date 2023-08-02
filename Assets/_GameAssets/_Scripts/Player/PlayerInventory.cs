using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
using HLProject.Weapons;
using HLProject.Scriptables;
using HLProject.Managers;

namespace HLProject.Characters
{
    [RequireComponent(typeof(Player))]
    public class PlayerInventory : NetworkBehaviour
    {
        static KeyCode[] WeaponCycleKeys = new KeyCode[6]
        {
            KeyCode.Alpha1, KeyCode.Alpha2,
            KeyCode.Alpha3, KeyCode.Alpha4,
            KeyCode.Alpha5, KeyCode.Alpha6
        };

        [SerializeField] Transform wWeaponPivot;
        [SerializeField] PlayerClientHands[] clientHands;

        [SyncVar] bool isServerInitialized, isSwappingWeapons;

        bool _DisablePlayerInputs;
        public bool DisablePlayerInputs
        {
            get => _DisablePlayerInputs;

            set
            {
                _DisablePlayerInputs = value;
                if (weaponsInventoryOnClient != null && weaponsInventoryOnClient.Count > 0 && currentWeaponIndex != -1)
                    weaponsInventoryOnClient[currentWeaponIndex].ToggleWeaponSway(!_DisablePlayerInputs);
            }
        }

        int currentWeaponIndex, currentCyclerIndex;
        WeaponType currentCyclerType;
        Player playerScript;
        Transform vWeaponPivot;
        PlayerClientHands currentClassHands;
        PlayerAnimationController animController;
        Coroutine swapWeaponRoutine;
        RaycastHit[] sphrCastHits;

        AsyncOperationHandle<GameObject> weaponPrefabHandle;

        List<Weapon> weaponsInventoryOnClient;
        List<int> weaponCyclerList;
        List<NetWeapon> weaponsInventoryOnServer;

        void Awake()
        {
            playerScript = GetComponent<Player>();
            animController = GetComponent<PlayerAnimationController>();
            sphrCastHits = new RaycastHit[5];
        }

        void LoadAsset()
        {
            weaponPrefabHandle = Addressables.LoadAssetAsync<GameObject>("Weapon_Server");
            weaponPrefabHandle.Completed += OnWeaponPrefabHandleComplete;
        }

        void OnWeaponPrefabHandleComplete(AsyncOperationHandle<GameObject> operation)
        {
            if (operation.Status == AsyncOperationStatus.Failed)
                Debug.LogErrorFormat("Couldn't load weapon prefab: {0}", operation.OperationException);
        }

        void OnDestroy()
        {
            Addressables.Release(weaponPrefabHandle);
        }

        public override void OnStartClient()
        {
            if (hasAuthority || !isServerInitialized) return;
            CmdRequestWeaponInventory();
        }

        public override void OnStartServer()
        {
            LoadAsset();
            playerScript.OnPlayerDead += OnPlayerDies;
        }

        void Start()
        {
            weaponsInventoryOnServer = new List<NetWeapon>();
            weaponsInventoryOnClient = new List<Weapon>();
            weaponCyclerList = new List<int>();
            vWeaponPivot = GameModeManager.INS.GetClientCamera().GetChild(0);
            currentCyclerIndex = -1;
        }

        void Update()
        {
            if (!isLocalPlayer) return;

            if (weaponsInventoryOnClient != null && currentWeaponIndex < weaponsInventoryOnClient.Count && weaponsInventoryOnClient[currentWeaponIndex] != null)
                weaponsInventoryOnClient[currentWeaponIndex].CheckPlayerMovement(playerScript.PlayerIsMoving(), playerScript.PlayerIsRunning());

            if (Utilities.MouseOverUI()) return;
            if (!DisablePlayerInputs) CheckInputs();
        }

        #region Server

        [Server]
        public void SetupWeaponInventory(ClientHandType handType, WeaponData[] weaponsToLoad, int defaultWeaponIndex)
        {
            weaponsInventoryOnServer.Clear();

            int size = weaponsToLoad.Length;
            for (int i = 0; i < size; i++)
            {
                Debug.LogFormat("Server: Spawn NetWeapon {0} of type {1}", weaponsToLoad[i].weaponName, weaponsToLoad[i].weaponType);
                GameObject weaponObject = Instantiate(weaponPrefabHandle.Result, wWeaponPivot);
                NetWeapon spawnedWeapon = weaponObject.GetComponent<NetWeapon>();

                if (connectionToClient != null) NetworkServer.Spawn(weaponObject, gameObject);
                else NetworkServer.Spawn(weaponObject);

                RpcSetWeaponParent(spawnedWeapon.netId, false);
                spawnedWeapon.Init(weaponsToLoad[i], playerScript);
                weaponsInventoryOnServer.Add(spawnedWeapon);
            }

            currentWeaponIndex = defaultWeaponIndex;
            playerScript.UpdateCurrentWeaponWeight(weaponsInventoryOnServer[currentWeaponIndex].GetWeaponData().weaponWeight);
            weaponsInventoryOnServer[currentWeaponIndex].RpcToggleClientWeapon(true);
            animController.OnPlayerChangesWeapons(weaponsInventoryOnServer[currentWeaponIndex].GetWeaponData().weaponName);

            Debug.LogFormat("Server: Default Weapon {0} of index {1}", weaponsInventoryOnServer[currentWeaponIndex].GetWeaponData().weaponName, currentWeaponIndex);
            print("Finished server PlayerInventory Initialization");

            isServerInitialized = true;
            if (connectionToClient != null) RpcSetupWeaponInventory(connectionToClient, size, defaultWeaponIndex, (int) handType);
        }

        [Server]
        void OnPlayerDies()
        {
            int size = weaponsInventoryOnServer.Count;
            for (int i = 0; i < size; i++) weaponsInventoryOnServer[i].DropClientWeaponAndDestroy();
            RpcClearWeaponInventory();
        }

        [Server]
        public void HideWeapons()
        {
            int size = weaponsInventoryOnServer.Count;
            for (int i = 0; i < size; i++) weaponsInventoryOnServer[i].HideWeapons();
            RpcHideWeapons(connectionToClient);
        }

        [Server]
        public Transform GetObjectFromPlayerAim()
        {
            Ray ray = new Ray(wWeaponPivot.position + wWeaponPivot.forward * 2, wWeaponPivot.forward);
            int quantity = Physics.SphereCastNonAlloc(ray, .35f, sphrCastHits);

            int closestIndex = -1;
            float closestDistance = 99999;
            for (int i = 0; i < quantity; i++)
            {
                float dist = Vector3.Distance(wWeaponPivot.position, sphrCastHits[i].point);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestIndex = i;
                }
            }

            if (closestIndex == -1) return null;
            return sphrCastHits[closestIndex].transform;
        }

        #endregion

        #region Client

        [Client]
        public void ForceReload()
        {
            weaponsInventoryOnClient[currentWeaponIndex].ForceReload();
        }

        [Client]
        void CheckInputs()
        {
            if (weaponsInventoryOnClient == null || currentWeaponIndex >= weaponsInventoryOnClient.Count || weaponsInventoryOnClient[currentWeaponIndex] == null) return;

            if (WeaponSelectorCycle() || isSwappingWeapons) return;

            //print($"Down: {Input.GetMouseButtonDown(0)} Up: {Input.GetMouseButtonUp(0)} - Pressed: {Input.GetMouseButton(0)}");

            if (Input.GetMouseButtonUp(0)) weaponsInventoryOnClient[currentWeaponIndex].EndFire();
            if (!playerScript.PlayerIsRunning())
            {
                if (Input.GetMouseButtonDown(0)) weaponsInventoryOnClient[currentWeaponIndex].InitFire();

                if (!Input.GetMouseButtonUp(0) && Input.GetMouseButton(0))
                {
                    weaponsInventoryOnClient[currentWeaponIndex].Fire();
                    UpdateWeaponAmmo();
                }

                if (Input.GetMouseButtonDown(1)) weaponsInventoryOnClient[currentWeaponIndex].ScopeIn();
            }

            if (Input.GetMouseButtonUp(1)) weaponsInventoryOnClient[currentWeaponIndex].ScopeOut();
            if (Input.GetKeyDown(KeyCode.R)) weaponsInventoryOnClient[currentWeaponIndex].Reload();
            if (Input.GetKeyDown(KeyCode.V)) weaponsInventoryOnClient[currentWeaponIndex].SwitchFireMode();
        }

        [Client]
        bool WeaponSelectorCycle()
        {
            if (!Input.anyKeyDown) return false;

            if (weaponCyclerList.Count > 0 && (Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0)))
            {
                bool checkWeaponIndex = currentCyclerIndex < weaponCyclerList.Count && weaponCyclerList[currentCyclerIndex] != currentWeaponIndex;
                bool success = false;
                if ((weaponsInventoryOnClient[currentWeaponIndex].WType != currentCyclerType) || checkWeaponIndex)
                {
                    bool onAltMode = currentCyclerType == WeaponType.AltMode || 
                        (currentCyclerType == WeaponType.Primary && weaponsInventoryOnClient[currentWeaponIndex].OnAltMode);

                    if (onAltMode) CmdRequestWeaponModeChange();
                    else CmdRequestWeaponChange(weaponCyclerList[currentCyclerIndex]);
                    isSwappingWeapons = true;
                    success = true;
                }

                //currentCyclerType = weaponsInvetoryOnClient[currentWeaponIndex].WType;
                currentCyclerIndex = -1;
                weaponCyclerList.Clear();
                return success;
            }

            int weaponIndex;
            int size = WeaponCycleKeys.Length;

            for (int i = 0; i < size; i++)
            {
                if (Input.GetKeyDown(WeaponCycleKeys[i]))
                {
                    WeaponType currentType = (WeaponType) i;
                    if (currentCyclerType == currentType)
                    {
                        if (SearchForWeaponsType(currentType, out weaponIndex))
                        {
                            weaponCyclerList.Add(weaponIndex);
                            currentCyclerIndex++;
                            break;
                        }

                        currentCyclerIndex++;
                        if (currentCyclerIndex >= weaponCyclerList.Count) currentCyclerIndex = 0;
                        break;
                    }

                    currentCyclerIndex = 0;
                    if (weaponCyclerList.Count > 0) weaponCyclerList.Clear();

                    if (currentType == WeaponType.AltMode)
                    {
                        SearchForWeaponsType(WeaponType.Primary, out weaponIndex);
                        //if (weaponsInvetoryOnClient[weaponIndex].GetWeaponData().alternateWeaponMode == null) continue;
                        weaponCyclerList.Add(weaponIndex);
                    }
                    else if (SearchForWeaponsType(currentType, out weaponIndex)) weaponCyclerList.Add(weaponIndex);
                    currentCyclerType = currentType;
                    break;
                }
            }

            return false;
        }

        [Client]
        bool SearchForWeaponsType(WeaponType TypeToSearch, out int weaponIndex)
        {
            weaponIndex = -1;
            int size = weaponsInventoryOnClient.Count;
            for (int i = 0; i < size; i++)
            {
                if ((weaponsInventoryOnClient[i].OnAltMode && TypeToSearch == WeaponType.Primary || weaponsInventoryOnClient[i].WType == TypeToSearch) && !weaponCyclerList.Contains(i))
                {
                    weaponIndex = i;
                    return true;
                }
            }

            return false;
        }

        [Client]
        void UpdateWeaponAmmo() => playerScript.PlayerCanvasScript.SetCurrentAmmo(weaponsInventoryOnClient[currentWeaponIndex].BulletsInMag, weaponsInventoryOnClient[currentWeaponIndex].Mags);

        [Client]
        void UpdateCurrenWeaponName() => playerScript.PlayerCanvasScript.SetCurrentWeapon(weaponsInventoryOnClient[currentWeaponIndex].GetWeaponData().weaponName);

        #endregion

        #region Commands

        [Command]
        public void CmdRequestWeaponModeChange()
        {
            isSwappingWeapons = true;

            int size = weaponsInventoryOnServer.Count, weaponIndex = -1;
            for (int i = 0; i < size; i++)
            {
                if (weaponsInventoryOnServer[i].WType == WeaponType.Primary || weaponsInventoryOnServer[i].LastWType == WeaponType.Primary)
                {
                    weaponIndex = i;
                    break;
                }
            }

            if (weaponIndex == -1) return;
            bool changeWeapon = weaponIndex != currentWeaponIndex;
            int oldIndex = currentWeaponIndex;
            if (changeWeapon)
            {
                weaponsInventoryOnServer[oldIndex].RpcToggleClientWeapon(false);
                weaponsInventoryOnServer[oldIndex].OnWeaponSwitch();
                weaponsInventoryOnServer[weaponIndex].RpcToggleClientWeapon(true);
                currentWeaponIndex = weaponIndex;
                playerScript.UpdateCurrentWeaponWeight(weaponsInventoryOnServer[currentWeaponIndex].GetWeaponData().weaponWeight);
            }

            weaponsInventoryOnServer[weaponIndex].ToggleAltMode();
            animController.OnPlayerChangesWeapons(weaponsInventoryOnServer[weaponIndex].GetWeaponData().weaponName);

            RpcChangeWeaponMode(connectionToClient, changeWeapon, weaponIndex, oldIndex);
            isSwappingWeapons = false;
        }

        [Command]
        public void CmdRequestWeaponChange(int weaponIndex)
        {
            if (weaponIndex == currentWeaponIndex) return;

            isSwappingWeapons = true;
            weaponsInventoryOnServer[currentWeaponIndex].RpcToggleClientWeapon(false);
            weaponsInventoryOnServer[currentWeaponIndex].OnWeaponSwitch();
            weaponsInventoryOnServer[weaponIndex].RpcToggleClientWeapon(true);

            RpcChangeWeapon(connectionToClient, weaponIndex, currentWeaponIndex);
            currentWeaponIndex = weaponIndex;
            animController.OnPlayerChangesWeapons(weaponsInventoryOnServer[currentWeaponIndex].GetWeaponData().weaponName);
            isSwappingWeapons = false;

            playerScript.UpdateCurrentWeaponWeight(weaponsInventoryOnServer[currentWeaponIndex].GetWeaponData().weaponWeight);
        }

        [Command(requiresAuthority = false)]
        void CmdRequestWeaponInventory()
        {
            int size = weaponsInventoryOnServer.Count;
            for (int i = 0; i < size; i++)
                RpcSetWeaponParent(weaponsInventoryOnServer[i].netId, i == currentWeaponIndex);
        }

        #endregion

        #region RPCs

        [TargetRpc]
        public void RpcChangeWeaponMode(NetworkConnection target, bool didChangeWeapon, int weaponIndex, int oldIndex)
        {
            if (didChangeWeapon)
            {
                if (swapWeaponRoutine != null) StopCoroutine(swapWeaponRoutine);
                swapWeaponRoutine = StartCoroutine(ChangeWeaponAndModeRoutine(weaponIndex, oldIndex));
                return;
            }

            if (swapWeaponRoutine != null) StopCoroutine(swapWeaponRoutine);
            swapWeaponRoutine = StartCoroutine(ChangeWeaponModeRoutine());
        }

        [TargetRpc]
        public void RpcChangeWeapon(NetworkConnection target, int weaponIndex, int oldIndex)
        {
            if (swapWeaponRoutine != null) StopCoroutine(swapWeaponRoutine);
            swapWeaponRoutine = StartCoroutine(ChangeWeaponRoutine(weaponIndex, oldIndex));
        }

        [TargetRpc]
        public void RpcHideWeapons(NetworkConnection target)
        {
            int size = weaponsInventoryOnClient.Count;
            for (int i = 0; i < size; i++) weaponsInventoryOnClient[i].ToggleWeaponVisibility(false);
            currentClassHands.gameObject.SetActive(false);
        }

        [ClientRpc]
        void RpcSetWeaponParent(uint weaponID, bool isCurrentWeapon)
        {
            if (!NetworkClient.spawned.ContainsKey(weaponID))
            {
                Debug.LogErrorFormat("Weapon with Network ID: {0} not found", weaponID);
                return;
            }

            NetworkIdentity weaponIdentity = NetworkClient.spawned[weaponID];
            //if (weaponIdentity.transform.parent == vWeaponPivot) return;

            NetWeapon spawnedWeapon = weaponIdentity.GetComponent<NetWeapon>();
            if (spawnedWeapon != null)
            {
                spawnedWeapon.MyTransform.SetParent(wWeaponPivot);
                spawnedWeapon.MyTransform.localPosition = Vector3.zero;
                spawnedWeapon.MyTransform.localRotation = Quaternion.identity;

                if (isCurrentWeapon)
                    StartCoroutine(WaitForWeaponSync(spawnedWeapon.MyTransform));
            }
            else
            {
                Debug.LogErrorFormat("Found weapon with Network ID {0}, but it does not have a NetWeapon Component!", weaponID);
            }
        }


        [TargetRpc]
        void RpcSetupWeaponInventory(NetworkConnection target, int weaponsToSpawn, int defaultWeaponIndex, int clientHandType)
        {
            print("Player: Setup Weapon Inventory");
            weaponsInventoryOnClient.Clear();

            StartCoroutine(WaitForPlayerWeaponSync(() =>
            {
                for (int i = 0; i < weaponsToSpawn; i++)
                {
                    NetWeapon netWeapon = wWeaponPivot.GetChild(i).GetComponent<NetWeapon>();
                    if (netWeapon != null)
                    {
                        GameObject weaponObject = new GameObject("Weapon");
                        Weapon spawnedWeapon = weaponObject.AddComponent<Weapon>();

                        spawnedWeapon.MyTransform.SetParent(vWeaponPivot);
                        spawnedWeapon.MyTransform.localPosition = Vector3.zero;
                        spawnedWeapon.MyTransform.localRotation = Quaternion.identity;

                        spawnedWeapon.Init(netWeapon.GetWeaponData(), netWeapon, playerScript);
                        spawnedWeapon.OnFinishedReload += UpdateWeaponAmmo;
                        Debug.LogFormat("Client: Spawn weapon {0} of type {1}", netWeapon.GetWeaponData().weaponName, netWeapon.GetWeaponData().weaponType);
                        weaponsInventoryOnClient.Add(spawnedWeapon);
                    }
                }

                if (currentClassHands != null) Destroy(currentClassHands);
                currentClassHands = Instantiate(clientHands[clientHandType]);
                currentClassHands.MyTransform.SetParent(vWeaponPivot);
                currentClassHands.MyTransform.localPosition = Vector3.zero;
                currentClassHands.MyTransform.localRotation = Quaternion.identity;

                currentWeaponIndex = defaultWeaponIndex;
                weaponsInventoryOnClient[currentWeaponIndex].ToggleWeapon(true);
                currentCyclerType = weaponsInventoryOnClient[currentWeaponIndex].GetWeaponData().weaponType;

                currentClassHands.GetWeaponHandBones(weaponsInventoryOnClient[currentWeaponIndex].WeaponRootBone, weaponsInventoryOnClient[currentWeaponIndex].ClientWeaponTransform);

                UpdateCurrenWeaponName();
                UpdateWeaponAmmo();

                Debug.LogFormat("Client: Default Weapon index {0}", currentWeaponIndex);
                print("Finished client PlayerInventory Initialization");
            }));
        }

        [ClientRpc]
        void RpcClearWeaponInventory()
        {
            int size = weaponsInventoryOnClient.Count;
            for (int i = 0; i < size; i++)
            {
                weaponsInventoryOnClient[i].DropWeapon();
                Destroy(weaponsInventoryOnClient[i].gameObject);
            }

            weaponsInventoryOnClient.Clear();
            if (currentClassHands != null) Destroy(currentClassHands);
        }

        #endregion

        #region Coroutines

        [Client]
        IEnumerator WaitForWeaponSync(Transform weaponToLookAt)
        {
            while (weaponToLookAt.childCount <= 0) yield return new WaitForSeconds(.1f);
            BaseClientWeapon clientWeapon = weaponToLookAt.GetComponentInChildren<BaseClientWeapon>(true);
            if (clientWeapon != null) clientWeapon.DrawWeapon();
            else Debug.LogError("Couln't find IWeapon Component or NetWeapon doesn't have a client weapon child!");
        }

        [Client]
        IEnumerator WaitForPlayerWeaponSync(System.Action OnWeaponSync)
        {
            int debug = 0, dataFoundCount = 0;
            while (wWeaponPivot.childCount == 0 || dataFoundCount != wWeaponPivot.childCount)
            {
                int size = wWeaponPivot.childCount;
                for (int i = 0; i < size; i++)
                {
                    NetWeapon nW = wWeaponPivot.GetChild(i).GetComponent<NetWeapon>();
                    if (nW == null || nW.GetWeaponData() == null) continue;
                    dataFoundCount++;
                }

                Debug.LogFormat("Player waiting for weaponSync {0}", debug++);
                yield return new WaitForSeconds(.1f);
            }

            OnWeaponSync?.Invoke();
        }

        [Client]
        IEnumerator ChangeWeaponRoutine(int weaponIndex, int oldIndex)
        {
            float timeToWait = (float) weaponsInventoryOnClient[oldIndex].ToggleWeapon(false);
            yield return new WaitForSeconds(timeToWait);

            weaponsInventoryOnClient[weaponIndex].ToggleWeapon(true);

            currentClassHands.GetWeaponHandBones(weaponsInventoryOnClient[weaponIndex].WeaponRootBone, weaponsInventoryOnClient[weaponIndex].ClientWeaponTransform);
            currentWeaponIndex = weaponIndex;
            isSwappingWeapons = false;

            UpdateCurrenWeaponName();
            UpdateWeaponAmmo();
        }

        [Client]
        IEnumerator ChangeWeaponModeRoutine()
        {
            float timeToWait = (float) weaponsInventoryOnClient[currentWeaponIndex].ToggleAltMode();
            yield return new WaitForSeconds(timeToWait);

            isSwappingWeapons = false;
            UpdateCurrenWeaponName();
            UpdateWeaponAmmo();
        }

        [Client]
        IEnumerator ChangeWeaponAndModeRoutine(int weaponIndex, int oldIndex)
        {
            float timeToWait = (float) weaponsInventoryOnClient[oldIndex].ToggleWeapon(false);
            yield return new WaitForSeconds(timeToWait);

            weaponsInventoryOnClient[weaponIndex].ToggleWeapon(true);

            currentClassHands.GetWeaponHandBones(weaponsInventoryOnClient[weaponIndex].WeaponRootBone, weaponsInventoryOnClient[weaponIndex].ClientWeaponTransform);
            currentWeaponIndex = weaponIndex;

            timeToWait = (float) weaponsInventoryOnClient[currentWeaponIndex].ToggleAltMode();
            yield return new WaitForSeconds(timeToWait);

            isSwappingWeapons = false;
            UpdateCurrenWeaponName();
            UpdateWeaponAmmo();
        }

        #endregion
    }
}
