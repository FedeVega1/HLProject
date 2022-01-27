using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(Player))]
public class PlayerInventory : NetworkBehaviour
{
    [SerializeField] Transform wWeaponPivot;
    [SerializeField] GameObject WeaponPrefab;

    [SyncVar] bool isServerInitialized;

    int currentWeaponIndex;
    Player playerScript;
    Transform vWeaponPivot;

    List<Weapon> weaponsInvetoryOnClient;
    List<NetWeapon> weaponsInventoryOnServer;

    void Awake()
    {
        playerScript = GetComponent<Player>();
    }

    public override void OnStartClient()
    {
        if (hasAuthority || !isServerInitialized) return;
        CmdRequestWeaponInventory();
    }

    void Start()
    {
        weaponsInventoryOnServer = new List<NetWeapon>();
        weaponsInvetoryOnClient = new List<Weapon>();
        vWeaponPivot = GameModeManager.INS.GetClientCamera().GetChild(0);
    }

    void Update()
    {
        if (isLocalPlayer) CheckInputs();
    }

    void CheckInputs()
    {
        if (weaponsInvetoryOnClient == null || currentWeaponIndex >= weaponsInvetoryOnClient.Count || weaponsInvetoryOnClient[currentWeaponIndex] == null) return;

        if (Input.GetMouseButton(0))
        {
            weaponsInvetoryOnClient[currentWeaponIndex].Fire();
        }

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonUp(0)) weaponsInvetoryOnClient[currentWeaponIndex].Scope();
    }

    [Server]
    public void SetupWeaponInventory(WeaponData[] weaponsToLoad, int defaultWeaponIndex)
    {
        weaponsInventoryOnServer.Clear();

        int size = weaponsToLoad.Length;
        for (int i = 0; i < size; i++)
        {
            GameObject weaponObject = Instantiate(WeaponPrefab, wWeaponPivot);
            NetWeapon spawnedWeapon = weaponObject.GetComponent<NetWeapon>();

            NetworkServer.Spawn(weaponObject, gameObject);
            RpcSetWeaponParent(spawnedWeapon.netId);
            spawnedWeapon.Init(weaponsToLoad[i], playerScript);
            weaponsInventoryOnServer.Add(spawnedWeapon);
        }

        isServerInitialized = true;
        RpcSetupWeaponInventory(connectionToClient, size, defaultWeaponIndex);
    }

    [ClientRpc]
    void RpcSetWeaponParent(uint weaponID)
    {
        if (!NetworkClient.spawned.ContainsKey(weaponID))
        {
            Debug.LogError($"Weapon with Network ID: {weaponID} not found");
            return;
        }

        NetworkIdentity weaponIdentity = NetworkClient.spawned[weaponID];
        if (weaponIdentity.transform.parent == vWeaponPivot) return;

        NetWeapon spawnedWeapon = weaponIdentity.GetComponent<NetWeapon>();
        if (spawnedWeapon != null)
        {
            spawnedWeapon.MyTransform.SetParent(wWeaponPivot);
            spawnedWeapon.MyTransform.localPosition = Vector3.zero;
            spawnedWeapon.MyTransform.localRotation = Quaternion.identity;
        }
        else
        {
            Debug.LogError($"Found weapon with Network ID {weaponID}, but it does not have a NetWeapon Component!");
        }
    }

    [TargetRpc]
    void RpcSetupWeaponInventory(NetworkConnection target, int weaponsToSpawn, int defaultWeaponIndex)
    {
        weaponsInvetoryOnClient.Clear();

        if (wWeaponPivot.childCount == 0) return;

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
                weaponsInvetoryOnClient.Add(spawnedWeapon);
            }
        }

        currentWeaponIndex = defaultWeaponIndex;
    }

    [Command(requiresAuthority = false)]
    void CmdRequestWeaponInventory()
    {
        int size = weaponsInventoryOnServer.Count;
        for (int i = 0; i < size; i++)
        {
            RpcSetWeaponParent(weaponsInventoryOnServer[i].netId);
        }
    }
}
