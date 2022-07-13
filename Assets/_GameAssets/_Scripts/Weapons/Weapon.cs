using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public enum BulletType { RayCast, Physics }
public enum WeaponType { Melee, Secondary, Primary, Tools, BandAids }

public interface IWeapon
{
    public void Init(bool isServer, WeaponData wData, BulletData data, GameObject propPrefab);

    public void Fire(Vector3 destination, bool didHit, bool lastBullet = false);
    public void AltFire(Vector3 destination, bool didHit);
    public void Scope();

    public void Reload();

    public void HolsterWeapon();
    public void DrawWeapon();

    public void ToggleAllViewModels(bool toggle);

    public void DropProp();

    public Transform GetVirtualPivot();
    public Transform GetWorldPivot();

    public void CheckPlayerMovement(bool isMoving, bool isRunning);
    public void OnCameraMovement(Vector2 axis);
}

public class Weapon : CachedTransform
{
    public WeaponType WType => weaponData.weaponType;

    double fireTime, switchingTime, reloadTime, wTime;
    public int BulletsInMag { get; private set; }
    public int Mags { get; private set; }

    RaycastHit rayHit;

    IWeapon clientWeapon;
    LayerMask weaponLayerMask;
    WeaponData weaponData;
    BulletData bulletData;
    Transform firePivot;
    NetWeapon netWeapon;

    public System.Action OnFinishedReload;

    void Start()
    {
        weaponLayerMask = LayerMask.GetMask("PlayerHitBoxes", "SceneObjects");
    }

    public void Init(WeaponData data, NetWeapon serverSideWeapon)
    {
        weaponData = data;
        bulletData = data.bulletData;
        netWeapon = serverSideWeapon;

        Mags = weaponData.mags;
        BulletsInMag = weaponData.bulletsPerMag;

        clientWeapon = Instantiate(weaponData.clientPrefab, MyTransform).GetComponent<IWeapon>();
        if (clientWeapon != null)
        {
            clientWeapon.Init(false, weaponData, bulletData, weaponData.propPrefab);
            firePivot = clientWeapon.GetVirtualPivot();
        }
        else
        {
            GameObject nullWGO = new GameObject($"{weaponData.weaponName} (NULL)");
            clientWeapon = nullWGO.AddComponent<NullWeapon>();
            clientWeapon.Init(false, weaponData, bulletData, weaponData.propPrefab);
            Debug.LogError($"Client weapon prefab does not have a IWeapon type component.\nSwitching to default weapon");
        }
    }

    public void Fire()
    {
        if (NetworkTime.time < wTime) return;
        if (BulletsInMag <= 0) return;

        Ray weaponRay = new Ray(firePivot.position, firePivot.forward);
        if (Physics.Raycast(weaponRay, out rayHit, bulletData.maxTravelDistance, weaponLayerMask))
        {
            Vector3 hitPos = rayHit.point;
            bool fallOffCheck = CheckBulletFallOff(ref weaponRay, ref rayHit, out float distance);

            if (!fallOffCheck) hitPos = rayHit.point;
            clientWeapon.Fire(hitPos, true, BulletsInMag < 2);

            if (rayHit.collider != null) print($"Client Fire! Range[{bulletData.maxTravelDistance}] - HitPoint: {rayHit.point}|{rayHit.collider.name}");
        }
        else
        {
            Vector3 fallOff = Vector3.down * bulletData.fallOff;
            clientWeapon.Fire(weaponRay.origin + ((weaponRay.direction + fallOff) * bulletData.maxTravelDistance), false, BulletsInMag < 2);

            Debug.DrawRay(weaponRay.origin, weaponRay.direction + fallOff, Color.red, 2);
            Debug.DrawLine(weaponRay.origin, weaponRay.origin + ((weaponRay.direction + fallOff) * bulletData.maxTravelDistance), Color.red, 2);
        }

        BulletsInMag--;
        wTime = NetworkTime.time + weaponData.weaponAnimsTiming.fire;
        netWeapon.CmdRequestFire();
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
                print($"Client Fire! Range[{distance}] - HitPoint: {rayHit.point}|{rayHit.collider.name}");
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

    public void AltFire()
    {
        clientWeapon.AltFire(Vector3.zero, false);
    }

    public void Scope()
    {
        clientWeapon.Scope();
    }

    public void Reload()
    {
        if (BulletsInMag >= weaponData.bulletsPerMag || Mags <= 0) return;
        clientWeapon.Reload();
        StartCoroutine(ReloadRoutine());
        netWeapon.CmdRequestReload();
    }

    IEnumerator ReloadRoutine()
    {
        yield return new WaitForSeconds(weaponData.weaponAnimsTiming.reload);
        BulletsInMag = weaponData.bulletsPerMag;
        Mags--;
        OnFinishedReload?.Invoke();
    }

    public float GetRateOfFire() => weaponData.rateOfFire;

    public void ToggleWeapon(bool toggle)
    {
        if (toggle)
        {
            clientWeapon.DrawWeapon();
            wTime = NetworkTime.time + weaponData.weaponAnimsTiming.draw;
        }
        else
        {
            clientWeapon.HolsterWeapon();
            wTime = NetworkTime.time + weaponData.weaponAnimsTiming.draw;
        }
    }

    public void DropWeapon() => clientWeapon.DropProp();

    public WeaponData GetWeaponData() => weaponData;

    public void CheckPlayerMovement(bool isMoving, bool isRunning) => clientWeapon.CheckPlayerMovement(isMoving, isRunning);
}
