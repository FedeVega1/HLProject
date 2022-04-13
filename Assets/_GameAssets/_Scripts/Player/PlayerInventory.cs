using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

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
    [SerializeField] GameObject WeaponPrefab;

    [SyncVar] bool isServerInitialized;

    public bool DisablePlayerInputs { get; set; }

    int currentWeaponIndex, currentCyclerIndex;
    WeaponType currentCyclerType;
    Player playerScript;
    Transform vWeaponPivot;

    List<Weapon> weaponsInvetoryOnClient;
    List<int> weaponCyclerList;
    List<NetWeapon> weaponsInventoryOnServer;

    void Awake() => playerScript = GetComponent<Player>();

    public override void OnStartClient()
    {
        if (hasAuthority || !isServerInitialized) return;
        CmdRequestWeaponInventory();
    }

    public override void OnStartServer()
    {
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
        if (isLocalPlayer && !DisablePlayerInputs) CheckInputs();
    }

    #region Server

    [Server]
    public void SetupWeaponInventory(WeaponData[] weaponsToLoad, int defaultWeaponIndex)
    {
        weaponsInventoryOnServer.Clear();

        int size = weaponsToLoad.Length;
        for (int i = 0; i < size; i++)
        {
            print($"Server: Spawn NetWeapon {weaponsToLoad[i].weaponName} of type {weaponsToLoad[i].weaponType}");
            GameObject weaponObject = Instantiate(WeaponPrefab, wWeaponPivot);
            NetWeapon spawnedWeapon = weaponObject.GetComponent<NetWeapon>();

            NetworkServer.Spawn(weaponObject, gameObject);
            RpcSetWeaponParent(spawnedWeapon.netId, false);
            spawnedWeapon.Init(weaponsToLoad[i], playerScript);
            weaponsInventoryOnServer.Add(spawnedWeapon);
        }

        currentWeaponIndex = defaultWeaponIndex;
        weaponsInventoryOnServer[currentWeaponIndex].RpcToggleClientWeapon(true);

        print($"Server: Default Weapon {weaponsInventoryOnServer[currentWeaponIndex].GetWeaponData().weaponName} of index {currentWeaponIndex}");
        print($"Finished server PlayerInventory Initialization");

        isServerInitialized = true;
        RpcSetupWeaponInventory(connectionToClient, size, defaultWeaponIndex);
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

        print("A");
        if (WeaponSelectorCycle()) return;

        print($"Down: {Input.GetMouseButtonDown(0)} Up: {Input.GetMouseButtonUp(0)} - Pressed: {Input.GetMouseButton(0)}");
        if (!Input.GetMouseButtonUp(0) && Input.GetMouseButton(0)) weaponsInvetoryOnClient[currentWeaponIndex].Fire();
        if (Input.GetMouseButton(1)) weaponsInvetoryOnClient[currentWeaponIndex].Scope();
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

    #endregion

    #region Commands

    [Command]
    public void CmdRequestWeaponChange(int weaponIndex)
    {
        if (weaponIndex == currentWeaponIndex) return;

        weaponsInventoryOnServer[currentWeaponIndex].RpcToggleClientWeapon(false);
        weaponsInventoryOnServer[weaponIndex].RpcToggleClientWeapon(true);

        RpcChangeWeapon(connectionToClient, weaponIndex, currentWeaponIndex);
        currentWeaponIndex = weaponIndex;
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
        currentWeaponIndex = weaponIndex;
    }

    [ClientRpc]
    void RpcSetWeaponParent(uint weaponID, bool isCurrentWeapon)
    {
        if (!NetworkClient.spawned.ContainsKey(weaponID))
        {
            Debug.LogError($"Weapon with Network ID: {weaponID} not found");
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
            Debug.LogError($"Found weapon with Network ID {weaponID}, but it does not have a NetWeapon Component!");
        }
    }


    [TargetRpc]
    void RpcSetupWeaponInventory(NetworkConnection target, int weaponsToSpawn, int defaultWeaponIndex)
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

                    spawnedWeapon.Init(netWeapon.GetWeaponData(), netWeapon);
                    print($"Client: Spawn weapon {netWeapon.GetWeaponData().weaponName} of type {netWeapon.GetWeaponData().weaponType}");
                    weaponsInvetoryOnClient.Add(spawnedWeapon);
                }
            }

            currentWeaponIndex = defaultWeaponIndex;
            weaponsInvetoryOnClient[currentWeaponIndex].ToggleWeapon(true);

            print($"Client: Default Weapon index {currentWeaponIndex}");
            print($"Finished client PlayerInventory Initialization");
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
        IWeapon clientWeapon = weaponToLookAt.GetComponentInChildren<IWeapon>(true);
        if (clientWeapon != null) clientWeapon.DrawWeapon();
        else Debug.LogError($"Couln't find IWeapon Component or NetWeapon doesn't have a client weapon child!");
    }

    [Client]
    IEnumerator WaitForPlayerWeaponSync(System.Action OnWeaponSync)
    {
        int debug = 0;
        while (wWeaponPivot.childCount == 0)
        {
            print($"Player waiting for weaponSync {debug++}");
            yield return new WaitForSeconds(.1f);
        }

        OnWeaponSync?.Invoke();
    }

    #endregion
}
