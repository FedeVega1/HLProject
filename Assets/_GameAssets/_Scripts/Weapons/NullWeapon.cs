using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NullWeapon : CachedTransform, IWeapon
{
    string[] stringCache = new string[2] { "Server", "Client" };

    public void Init(bool isServer, BulletData data) { Debug.LogError($"Initialized {name} as a NULL Weapon on {stringCache[isServer ? 0 : 1]}"); }
    public void ToggleAllViewModels(bool toggle) { }

    public void Fire(Vector3 destination) { }

    public void AltFire(Vector3 destination) { }

    public void Scope() { }

    public void DrawWeapon() { }

    public void HolsterWeapon() { }

    public Transform GetVirtualPivot() => MyTransform;
    public Transform GetWorldPivot() => MyTransform;
}
