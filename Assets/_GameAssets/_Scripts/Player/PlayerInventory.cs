using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Player))]
public class PlayerInventory : CommonNetworkBehaviour
{
    static KeyCode[] WeaponCycleKeys = new KeyCode[5] 
    { 
        KeyCode.Alpha1, KeyCode.Alpha2,
        KeyCode.Alpha3, KeyCode.Alpha4,
        KeyCode.Alpha5,
    };

    [SerializeField] Transform wWeaponPivot;
    [SerializeField] GameObject WeaponPrefab;

    NetworkVariable<bool> isServerInitialized = new NetworkVariable<bool>(), isSwappingWeapons = new NetworkVariable<bool>();

    public bool DisablePlayerInputs { get; set; }

    int currentWeaponIndex, currentCyclerIndex;
    WeaponType currentCyclerType;
    Player playerScript;
    Transform vWeaponPivot;

    List<Weapon> weaponsInvetoryOnClient;
    List<int> weaponCyclerList;
    List<NetWeapon> weaponsInventoryOnServer;

    void Awake() => playerScript = GetComponent<Player>();

    protected override void OnClientSpawn()
    {
        if (IsOwner || !isServerInitialized.Value) return;
        ServerRpcParams rpcParams = new ServerRpcParams { Receive = new ServerRpcReceiveParams { SenderClientId = NetworkBehaviourId } };
        RequestWeaponInventory_ServerRpc(rpcParams);
    }

    protected override void OnServerSpawn()
    {
        playerScript.OnPlayerDead += OnPlayerDies_Server;
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
        if (!IsLocalPlayer) return;

        if (weaponsInvetoryOnClient != null && currentWeaponIndex < weaponsInvetoryOnClient.Count && weaponsInvetoryOnClient[currentWeaponIndex] != null)
            weaponsInvetoryOnClient[currentWeaponIndex].CheckPlayerMovement(playerScript.PlayerIsMoving(), playerScript.PlayerIsRunning());

        if (Utilities.MouseOverUI()) return;
        if (!DisablePlayerInputs) CheckInputs_Client();
    }

    #region Server

    public void SetupWeaponInventory_Server(WeaponData[] weaponsToLoad, int defaultWeaponIndex)
    {
        weaponsInventoryOnServer.Clear();

        int size = weaponsToLoad.Length;
        for (int i = 0; i < size; i++)
        {
            Debug.LogFormat("Server: Spawn NetWeapon {0} of type {1}", weaponsToLoad[i].weaponName, weaponsToLoad[i].weaponType);
            GameObject weaponObject = Instantiate(WeaponPrefab, wWeaponPivot);
            NetWeapon spawnedWeapon = weaponObject.GetComponent<NetWeapon>();

            if (IsSpawned) weaponObject.GetComponent<NetworkObject>().SpawnWithOwnership(NetworkObjectId);
            else weaponObject.GetComponent<NetworkObject>().Spawn();
            
            SetWeaponParent_ClientRpc(spawnedWeapon.NetworkBehaviourId, false);
            spawnedWeapon.Init(weaponsToLoad[i], playerScript);
            weaponsInventoryOnServer.Add(spawnedWeapon);
        }

        currentWeaponIndex = defaultWeaponIndex;
        weaponsInventoryOnServer[currentWeaponIndex].RpcToggleClientWeapon(true);

        Debug.LogFormat("Server: Default Weapon {0} of index {1}", weaponsInventoryOnServer[currentWeaponIndex].GetWeaponData().weaponName, currentWeaponIndex);
        print("Finished server PlayerInventory Initialization");

        isServerInitialized.Value = true;
        if (IsSpawned)
        {
            ClientRpcParams rpcParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { NetworkBehaviourId } } };
            SetupWeaponInventory_ClientRpc(size, defaultWeaponIndex, rpcParams);
        }
    }

    void OnPlayerDies_Server()
    {
        int size = weaponsInventoryOnServer.Count;
        for (int i = 0; i < size; i++) weaponsInventoryOnServer[i].DropClientWeaponAndDestroy();
        ClearWeaponInventory_ClientRpc();
    }

    #endregion

    #region Client

    void CheckInputs_Client()
    {
        if (weaponsInvetoryOnClient == null || currentWeaponIndex >= weaponsInvetoryOnClient.Count || weaponsInvetoryOnClient[currentWeaponIndex] == null) return;

        if (WeaponSelectorCycle_Client() || isSwappingWeapons.Value) return;

        //print($"Down: {Input.GetMouseButtonDown(0)} Up: {Input.GetMouseButtonUp(0)} - Pressed: {Input.GetMouseButton(0)}");
		
        if (!Input.GetMouseButtonUp(0) && Input.GetMouseButton(0))        
		{
            weaponsInvetoryOnClient[currentWeaponIndex].Fire();
            UpdateWeaponAmmo_Client();
        }
		
        if (Input.GetMouseButton(1)) weaponsInvetoryOnClient[currentWeaponIndex].Scope();
        if (Input.GetKeyDown(KeyCode.R)) weaponsInvetoryOnClient[currentWeaponIndex].Reload();
    }

    bool WeaponSelectorCycle_Client()
    {
        if (!Input.anyKeyDown) return false;

        if (weaponCyclerList.Count > 0 && (Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0)))
        {
            bool checkWeaponIndex = currentCyclerIndex < weaponCyclerList.Count && weaponCyclerList[currentCyclerIndex] != currentWeaponIndex;
            bool success = false;
            if (weaponsInvetoryOnClient[currentWeaponIndex].WType != currentCyclerType || checkWeaponIndex)
            {
                isSwappingWeapons.Value = true;
                RequestWeaponChange_ServerRpc(weaponCyclerList[currentCyclerIndex]);
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
                    if (SearchForWeaponsType_Client((WeaponType) i, out weaponIndex))
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
                if (SearchForWeaponsType_Client((WeaponType) i, out weaponIndex)) weaponCyclerList.Add(weaponIndex);
                currentCyclerType = (WeaponType) i;
                break;
            }
        }

        return false;
    }

    bool SearchForWeaponsType_Client(WeaponType TypeToSearch, out int weaponIndex)
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

    void UpdateWeaponAmmo_Client() => playerScript.PlayerCanvasScript.SetCurrentAmmo(weaponsInvetoryOnClient[currentWeaponIndex].BulletsInMag, weaponsInvetoryOnClient[currentWeaponIndex].Mags);

    void UpdateCurrenWeaponName_Client() => playerScript.PlayerCanvasScript.SetCurrentWeapon(weaponsInvetoryOnClient[currentWeaponIndex].GetWeaponData().weaponName);

    #endregion

    #region Commands

    [ServerRpc]
    public void RequestWeaponChange_ServerRpc(int weaponIndex)
    {
        if (weaponIndex == currentWeaponIndex) return;

        isSwappingWeapons.Value = true;
        weaponsInventoryOnServer[currentWeaponIndex].RpcToggleClientWeapon(false);
        weaponsInventoryOnServer[weaponIndex].RpcToggleClientWeapon(true);

        ClientRpcParams rpcParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { NetworkBehaviourId } } };
        ChangeWeapon_ClientRpc(weaponIndex, currentWeaponIndex, rpcParams);
        currentWeaponIndex = weaponIndex;
        isSwappingWeapons.Value = false;
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestWeaponInventory_ServerRpc(ServerRpcParams rpcParams)
    {
        if (!NetworkManager.ConnectedClients.ContainsKey(rpcParams.Receive.SenderClientId)) return;
        int size = weaponsInventoryOnServer.Count;
        for (int i = 0; i < size; i++)
            SetWeaponParent_ClientRpc(weaponsInventoryOnServer[i].NetworkBehaviourId, i == currentWeaponIndex);
    }

    #endregion

    #region RPCs

    [ClientRpc]
    public void ChangeWeapon_ClientRpc(int weaponIndex, int oldIndex, ClientRpcParams rpcParams)
    {
        weaponsInvetoryOnClient[oldIndex].ToggleWeapon(false);
        weaponsInvetoryOnClient[weaponIndex].ToggleWeapon(true);
        currentWeaponIndex = weaponIndex;
        isSwappingWeapons.Value = false;

        UpdateCurrenWeaponName_Client();
        UpdateWeaponAmmo_Client();
    }

    [ClientRpc]
    void SetWeaponParent_ClientRpc(uint weaponID, bool isCurrentWeapon)
    {
        if (!NetworkManager.SpawnManager.SpawnedObjects.ContainsKey(weaponID))
        {
            Debug.LogErrorFormat("Weapon with Network ID: {0} not found", weaponID);
            return;
        }

        NetworkObject weaponNetObject = NetworkManager.SpawnManager.SpawnedObjects[weaponID];
        //if (weaponIdentity.transform.parent == vWeaponPivot) return;

        NetWeapon spawnedWeapon = weaponNetObject.GetComponent<NetWeapon>();
        if (spawnedWeapon != null)
        {
            spawnedWeapon.MyTransform.SetParent(wWeaponPivot);
            spawnedWeapon.MyTransform.localPosition = Vector3.zero;
            spawnedWeapon.MyTransform.localRotation = Quaternion.identity;

            if (isCurrentWeapon)
                StartCoroutine(WaitForWeaponSync_Client(spawnedWeapon.MyTransform));
        }
        else
        {
            Debug.LogError($"Found weapon with Network ID {weaponID}, but it does not have a NetWeapon Component!");
        }
    }


    [ClientRpc]
    void SetupWeaponInventory_ClientRpc(int weaponsToSpawn, int defaultWeaponIndex, ClientRpcParams rpcParams)
    {
        print("Player: Setup Weapon Inventory");
        weaponsInvetoryOnClient.Clear();

        StartCoroutine(WaitForPlayerWeaponSync_Client(() =>
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

                    spawnedWeapon.Init(netWeapon.GetWeaponData(), netWeapon);
                    spawnedWeapon.OnFinishedReload += UpdateWeaponAmmo_Client;
                    Debug.LogFormat("Client: Spawn weapon {0} of type {1}", netWeapon.GetWeaponData().weaponName, netWeapon.GetWeaponData().weaponType);
                    weaponsInvetoryOnClient.Add(spawnedWeapon);
                }
            }

            currentWeaponIndex = defaultWeaponIndex;
            weaponsInvetoryOnClient[currentWeaponIndex].ToggleWeapon(true);

            UpdateCurrenWeaponName_Client();
            UpdateWeaponAmmo_Client();

            Debug.LogFormat("Client: Default Weapon index {0}", currentWeaponIndex);
            print("Finished client PlayerInventory Initialization");
        }));
    }

    [ClientRpc]
    void ClearWeaponInventory_ClientRpc()
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

    IEnumerator WaitForWeaponSync_Client(Transform weaponToLookAt)
    {
        while (weaponToLookAt.childCount <= 0) yield return new WaitForSeconds(.1f); 
        IWeapon clientWeapon = weaponToLookAt.GetComponentInChildren<IWeapon>(true);
        if (clientWeapon != null) clientWeapon.DrawWeapon();
        else Debug.LogError("Couln't find IWeapon Component or NetWeapon doesn't have a client weapon child!");
    }

    IEnumerator WaitForPlayerWeaponSync_Client(System.Action OnWeaponSync)
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
