using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseClientWeapon : CachedTransform
{
    protected enum ActiveViewModel { World, Virtual }

    [SerializeField] protected GameObject[] viewModels;
    [SerializeField] protected Transform virtualBulletPivot, worldBulletPivot;
    [SerializeField] protected float weaponSwayAmmount = 0.02f, weaponSwaySmooth = 5;
    [SerializeField] protected AudioSource virtualAudioSource, worldAudioSource, virtualMovementSource;
    [SerializeField] protected AudioClip[] weaponWalkSounds, weaponSprintSounds, weaponMovSounds;

    protected bool isServer, isDrawn;
    protected ActiveViewModel currentActiveViewModel;
    //protected Quaternion cameraTargetRotation, currentTargetRotation;
    protected Vector2 swayFactor;
    protected Quaternion defaultWeaponRotation, lastCameraRotation;

    protected GameObject weaponPropPrefab;
    protected BulletData bulletData;
    protected WeaponData weaponData;

    public virtual void Init(bool isServer, WeaponData wData, BulletData data, GameObject propPrefab)
    {
        bulletData = data;
        weaponData = wData;

        weaponPropPrefab = propPrefab;
        ToggleAllViewModels(false);

        currentActiveViewModel = isServer ? ActiveViewModel.World : ActiveViewModel.Virtual;
        viewModels[(int) currentActiveViewModel].SetActive(true);

        this.isServer = isServer;
        gameObject.SetActive(false);
    }

    protected virtual void Start()
    {
        defaultWeaponRotation = MyTransform.localRotation;
    }

    protected virtual void Update()
    {
        if (!isDrawn) return;

        swayFactor.x = Input.GetAxis("Mouse Y") * weaponSwayAmmount;
        swayFactor.y = -Input.GetAxis("Mouse X") * weaponSwayAmmount;

        Quaternion sway = Quaternion.Euler(defaultWeaponRotation.x + swayFactor.x, defaultWeaponRotation.y + swayFactor.y, 0);
        MyTransform.localRotation = Quaternion.Slerp(MyTransform.localRotation, sway, Time.deltaTime * weaponSwaySmooth);
        //currentTargetRotation = Quaternion.Slerp(currentTargetRotation, cameraTargetRotation, Time.deltaTime * CameraLerpTime());
        //MyTransform.rotation = currentTargetRotation;
    }

    public abstract void Fire(Vector3 destination, bool didHit, int ammo);
    public abstract void EmptyFire();

    public abstract void AltFire(Vector3 destination, bool didHit);
    public abstract void Scope();

    public abstract void Reload();

    public abstract void HolsterWeapon();
    public abstract void DrawWeapon();

    public virtual void ToggleAllViewModels(bool toggle)
    {
        int size = viewModels.Length;
        for (byte i = 0; i < size; i++) viewModels[i].SetActive(toggle);
    }

    public abstract void DropProp();

    public virtual Transform GetVirtualPivot() => MyTransform;
    public virtual Transform GetWorldPivot() => MyTransform;

    public abstract void CheckPlayerMovement(bool isMoving, bool isRunning);
}
