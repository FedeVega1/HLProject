using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerInventory : NetworkBehaviour
{
    [SerializeField] Transform weaponPivot;

    List<Weapon> weaponsOnInventory;
    int currentWeaponIndex;
    float weaponRateOfFire;

    void Start()
    {
        weaponsOnInventory = new List<Weapon>();
    }

    void Update()
    {
        if (isLocalPlayer) CheckInputs();
    }

    void CheckInputs()
    {
        if (Input.GetMouseButtonDown(0))
        {

        }

        if (Input.GetMouseButton(0))
        {

        }

        if (Input.GetMouseButtonUp(0))
        {

        }
    }

    public void SetupWeaponInventory(WeaponData[] weaponsToLoad, int defaultWeaponIndex)
    {
        int size = weaponsToLoad.Length;
        for (int i = 0; i < size; i++)
        {
            Weapon weaponScript = null;
            if (isClient)
            {
                GameObject weaponObject = Instantiate(weaponsToLoad[i].clientPrefab, weaponPivot.position, weaponPivot.rotation, weaponPivot);
                if (isLocalPlayer) weaponScript = weaponObject.AddComponent<Weapon>();
            }
            else
            {
                GameObject weaponObject = new GameObject(weaponsToLoad[i].weaponName);
                weaponScript = weaponObject.AddComponent<Weapon>();
            }

            if (weaponScript == null)
            {
                Debug.LogError($"Instantiated {weaponsToLoad[i].weaponName} weapon without a Weapon script");
                return;
            }

            weaponsOnInventory.Add(weaponScript);
        }
    }

    //[Server]
    //void Register

    [Command]
    void CmdFireWeapon()
    {

    }

    [Command]
    void CmdReleaseFireWeapon()
    {

    }

    [TargetRpc]
    void RpcRegisterWeapon(NetworkConnection conn, uint netID)
    {
       // weaponsOnInventory.Add(NetworkServerGet);
    }
}
