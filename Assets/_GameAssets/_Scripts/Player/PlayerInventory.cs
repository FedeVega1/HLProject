using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(Player))]
public class PlayerInventory : NetworkBehaviour
{
    [SerializeField] Transform wWeaponPivot;
    [SerializeField] GameObject WeaponPrefab;

    int currentWeaponIndex;
    Player playerScript;
    Transform vWeaponPivot;
    List<Weapon> weaponsOnInventory;

    void Awake()
    {
        playerScript = GetComponent<Player>();
    }

    void Start()
    {
        weaponsOnInventory = new List<Weapon>();
        vWeaponPivot = GameModeManager.INS.GetClientCamera().GetChild(0);
    }

    void Update()
    {
        if (isLocalPlayer) CheckInputs();
    }

    void CheckInputs()
    {
        if (weaponsOnInventory == null || currentWeaponIndex >= weaponsOnInventory.Count || weaponsOnInventory[currentWeaponIndex] == null) return;

        if (Input.GetMouseButton(0))
        {
            weaponsOnInventory[currentWeaponIndex].Fire();
        }

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonUp(0)) weaponsOnInventory[currentWeaponIndex].Scope();
    }

    [Server]
    public void SetupWeaponInventory(WeaponData[] weaponsToLoad, int defaultWeaponIndex)
    {
        int size = weaponsToLoad.Length;
        for (int i = 0; i < size; i++)
        {
            GameObject weaponObject = Instantiate(WeaponPrefab, wWeaponPivot);
            NetWeapon spawnedWeapon = weaponObject.GetComponent<NetWeapon>();

            NetworkServer.Spawn(weaponObject, gameObject);
            spawnedWeapon.Init(weaponsToLoad[i], playerScript);
        }

        RpcSetupWeaponInventory(connectionToClient, size, defaultWeaponIndex);
    }

    [TargetRpc]
    void RpcSetupWeaponInventory(NetworkConnection target, int weaponsToSpawn, int defaultWeaponIndex)
    {
        weaponsOnInventory.Clear();

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
                weaponsOnInventory.Add(spawnedWeapon);
            }
        }

        currentWeaponIndex = defaultWeaponIndex;
    }
}
