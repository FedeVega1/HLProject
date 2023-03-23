using UnityEngine;

public class NullWeapon : CachedTransform, IWeapon
{
    string[] stringCache = new string[2] { "Server", "Client" };

    public Quaternion CameraTargetRotation { get; set; }

    public void Init(bool isServer, WeaponData wData, BulletData data, GameObject propPrefab) { Debug.LogError($"Initialized {name} as a NULL Weapon on {stringCache[isServer ? 0 : 1]}"); }

    public void ToggleAllViewModels(bool toggle) { }
    public void Fire(Vector3 destination, bool didHit, bool lastBullet = false) { }
    public void AltFire(Vector3 destination, bool didHit) { }
    public void Scope() { }
    public void Reload() { }
    public void DrawWeapon() { }
    public void HolsterWeapon() { }
    public void DropProp() { }

    public Transform GetVirtualPivot() => MyTransform;
    public Transform GetWorldPivot() => MyTransform;

    public void CheckPlayerMovement(bool isMoving, bool isRunning) { }

    public float CameraLerpTime() => 5;
}
