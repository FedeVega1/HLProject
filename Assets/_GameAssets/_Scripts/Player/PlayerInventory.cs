using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;

[RequireComponent(typeof(Player))]
public class PlayerInventory : NetworkBehaviour
{
    static KeyCode[] WeaponCycleKeys = new KeyCode[5] 
    { 
        KeyCode.Alpha1, KeyCode.Alpha2,
        KeyCode.Alpha3, KeyCode.Alpha4,
        KeyCode.Alpha5,
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
            if (weaponsInvetoryOnClient != null && weaponsInvetoryOnClient.Count > 0 && currentWeaponIndex != -1) 
                weaponsInvetoryOnClient[currentWeaponIndex].ToggleWeaponSway(!_DisablePlayerInputs);
        }
    }
    
    int currentWeaponIndex, currentCyclerIndex;
    WeaponType currentCyclerType;
    Player playerScript;
    Transform vWeaponPivot;
    PlayerClientHands currentClassHands;

    AsyncOperationHandle<GameObject> weaponPrefabHandle;

    List<Weapon> weaponsInvetoryOnClient;
    List<int> weaponCyclerList;
    List<NetWeapon> weaponsInventoryOnServer;

    void Awake() => playerScript = GetComponent<Player>();

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
        weaponsInvetoryOnClient = new List<Weapon>();
        weaponCyclerList = new List<int>();
        vWeaponPivot = GameModeManager.INS.GetClientCamera().GetChild(0);
        currentCyclerIndex = -1;
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        if (weaponsInvetoryOnClient != null && currentWeaponIndex < weaponsInvetoryOnClient.Count && weaponsInvetoryOnClient[currentWeaponIndex] != null)
            weaponsInvetoryOnClient[currentWeaponIndex].CheckPlayerMovement(playerScript.PlayerIsMoving(), playerScript.PlayerIsRunning());

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

    #endregion

    #region Client

    [Client]
    void CheckInputs()
    {
        if (weaponsInvetoryOnClient == null || currentWeaponIndex >= weaponsInvetoryOnClient.Count || weaponsInvetoryOnClient[currentWeaponIndex] == null) return;

        if (WeaponSelectorCycle() || isSwappingWeapons) return;

        //print($"Down: {Input.GetMouseButtonDown(0)} Up: {Input.GetMouseButtonUp(0)} - Pressed: {Input.GetMouseButton(0)}");
		
        if (!playerScript.PlayerIsRunning())
        {
            if (!Input.GetMouseButtonUp(0) && Input.GetMouseButton(0))
            {
                weaponsInvetoryOnClient[currentWeaponIndex].Fire();
                UpdateWeaponAmmo();
            }

            if (Input.GetMouseButtonDown(1)) weaponsInvetoryOnClient[currentWeaponIndex].ScopeIn();
        }

        if (Input.GetMouseButtonUp(1)) weaponsInvetoryOnClient[currentWeaponIndex].ScopeOut();
        if (Input.GetKeyDown(KeyCode.R)) weaponsInvetoryOnClient[currentWeaponIndex].Reload();
    }

    [Client]
    bool WeaponSelectorCycle()
    {
        if (!Input.anyKeyDown) return false;

        if (weaponCyclerList.Count > 0 && (Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0)))
        {
            bool checkWeaponIndex = currentCyclerIndex < weaponCyclerList.Count && weaponCyclerList[currentCyclerIndex] != currentWeaponIndex;
            bool success = false;
            if (weaponsInvetoryOnClient[currentWeaponIndex].WType != currentCyclerType || checkWeaponIndex)
            {
                isSwappingWeapons = true;
                CmdRequestWeaponChange(weaponCyclerList[currentCyclerIndex]);
                success = true;
            }

            currentCyclerType = weaponsInvetoryOnClient[currentWeaponIndex].WType;
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
                if (currentCyclerType == (WeaponType) i)
                {
                    if (SearchForWeaponsType((WeaponType) i, out weaponIndex))
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
                if (SearchForWeaponsType((WeaponType) i, out weaponIndex)) weaponCyclerList.Add(weaponIndex);
                currentCyclerType = (WeaponType) i;
                break;
            }
        }

        return false;
    }

    [Client]
    bool SearchForWeaponsType(WeaponType TypeToSearch, out int weaponIndex)
    {
        weaponIndex = -1;
        int size = weaponsInvetoryOnClient.Count;
        for (int i = 0; i < size; i++)
        {
            if (weaponsInvetoryOnClient[i].WType == TypeToSearch && !weaponCyclerList.Contains(i))
            {
                weaponIndex = i;
                return true;
            }
        }

        return false;
    }

    [Client]
    void UpdateWeaponAmmo() => playerScript.PlayerCanvasScript.SetCurrentAmmo(weaponsInvetoryOnClient[currentWeaponIndex].BulletsInMag, weaponsInvetoryOnClient[currentWeaponIndex].Mags);

    [Client]
    void UpdateCurrenWeaponName() => playerScript.PlayerCanvasScript.SetCurrentWeapon(weaponsInvetoryOnClient[currentWeaponIndex].GetWeaponData().weaponName);

    #endregion

    #region Commands

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
    public void RpcChangeWeapon(NetworkConnection target, int weaponIndex, int oldIndex)
    {
        weaponsInvetoryOnClient[oldIndex].ToggleWeapon(false);
        weaponsInvetoryOnClient[weaponIndex].ToggleWeapon(true);
        currentClassHands.GetWeaponHandBones(weaponsInvetoryOnClient[weaponIndex].WeaponRootBone, weaponsInvetoryOnClient[weaponIndex].ClientWeaponTransform);
        currentWeaponIndex = weaponIndex;
        isSwappingWeapons = false;

        UpdateCurrenWeaponName();
        UpdateWeaponAmmo();
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
        weaponsInvetoryOnClient.Clear();

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
                    weaponsInvetoryOnClient.Add(spawnedWeapon);
                }
            }

            if (currentClassHands != null) Destroy(currentClassHands);
            currentClassHands = Instantiate(clientHands[clientHandType]);
            currentClassHands.MyTransform.SetParent(vWeaponPivot);
            currentClassHands.MyTransform.localPosition = Vector3.zero;
            currentClassHands.MyTransform.localRotation = Quaternion.identity;

            currentWeaponIndex = defaultWeaponIndex;
            weaponsInvetoryOnClient[currentWeaponIndex].ToggleWeapon(true);

            currentClassHands.GetWeaponHandBones(weaponsInvetoryOnClient[currentWeaponIndex].WeaponRootBone, weaponsInvetoryOnClient[currentWeaponIndex].ClientWeaponTransform);

            UpdateCurrenWeaponName();
            UpdateWeaponAmmo();

            Debug.LogFormat("Client: Default Weapon index {0}", currentWeaponIndex);
            print("Finished client PlayerInventory Initialization");
        }));
    }

    [ClientRpc]
    void RpcClearWeaponInventory()
    {
        int size = weaponsInvetoryOnClient.Count;
        for (int i = 0; i < size; i++)
        {
            weaponsInvetoryOnClient[i].DropWeapon();
            Destroy(weaponsInvetoryOnClient[i].gameObject);
        }

        weaponsInvetoryOnClient.Clear();
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
        int debug = 0;
        while (wWeaponPivot.childCount == 0)
        {
            Debug.LogFormat("Player waiting for weaponSync {0}", debug++);
            yield return new WaitForSeconds(.1f);
        }

        OnWeaponSync?.Invoke();
    }

    #endregion
}
