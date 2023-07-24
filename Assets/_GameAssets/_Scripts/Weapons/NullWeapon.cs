using UnityEngine;

namespace HLProject
{
    public class NullWeapon : BaseClientWeapon
    {
        string[] stringCache = new string[2] { "Server", "Client" };

        public Quaternion CameraTargetRotation { get; set; }

        public override void Init(bool isServer, WeaponData wData, BulletData data, GameObject propPrefab) { Debug.LogErrorFormat("Initialized {0} as a NULL Weapon on {1}", name, stringCache[isServer ? 0 : 1]); }

        public override void ToggleAllViewModels(bool toggle) { }
        public override void Fire(Vector3 destination, bool didHit, int ammo) { }
        public override void EmptyFire() { }
        public override void AltFire(Vector3 destination, bool didHit, int ammo) { }
        public override void ScopeIn() { }
        public override void ScopeOut() { }
        public override void Reload(int bulletsToReload) { }
        public override void DrawWeapon() { }
        public override void HolsterWeapon() { }
        public override void DropProp() { }
        public override void OnAltMode(bool toggle) { }

        public override void CheckPlayerMovement(bool isMoving, bool isRunning) { }
    }
}
