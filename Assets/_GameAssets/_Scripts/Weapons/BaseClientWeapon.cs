using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseClientWeapon : CachedTransform
{
    protected enum ActiveViewModel { World, Virtual }

    [SerializeField] protected GameObject[] viewModels;
    [SerializeField] protected Transform virtualBulletPivot, worldBulletPivot;
    [SerializeField] protected float weaponSwayAmmount = 0.02f, weaponSwaySmooth = 5, zoomInSpeed, zoomOutSpeed;
    [SerializeField] protected AudioSource virtualAudioSource, worldAudioSource, virtualMovementSource;
    [SerializeField] protected AudioClip[] weaponWalkSounds, weaponSprintSounds, weaponMovSounds;
    [SerializeField] protected Vector3 aimPosition;
    [SerializeField] protected Quaternion aimRotation;

    protected bool isServer, isDrawn, enableWeaponSway, onScopeAim;
    protected ActiveViewModel currentActiveViewModel;
    protected int scopeID = -1;
    protected Vector3 defaultWeaponPos;
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
        defaultWeaponPos = MyTransform.localPosition;
        enableWeaponSway = true;
    }

    protected virtual void Update()
    {
        if (!isDrawn || !enableWeaponSway) return;

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

    public virtual void ScopeIn()
    {
        if (!isDrawn || onScopeAim) return;
        enableWeaponSway = false;
        onScopeAim = true;

        Vector3 currentPos = MyTransform.localPosition;
        Quaternion currentRot = MyTransform.localRotation;

        if (scopeID != -1) LeanTween.cancel(scopeID);
        scopeID = LeanTween.value(0, 1, zoomInSpeed).setOnUpdate((float x) =>
        {
            GameModeManager.INS.SetClientVCameraFOV(Mathf.Lerp(90, 60, x));
            MyTransform.localPosition = Vector3.Lerp(currentPos, aimPosition, x);
            MyTransform.localRotation = Quaternion.Lerp(currentRot, aimRotation, x);
        }).setOnComplete(() => scopeID = -1).uniqueId;
    }

    public virtual void ScopeOut()
    {
        if (!isDrawn || !onScopeAim) return;
        enableWeaponSway = true;
        onScopeAim = false;

        Vector3 currentPos = MyTransform.localPosition;
        Quaternion currentRot = MyTransform.localRotation;

        if (scopeID != -1) LeanTween.cancel(scopeID);
        scopeID = LeanTween.value(0, 1, zoomOutSpeed).setOnUpdate((float x) =>
        {
            GameModeManager.INS.SetClientVCameraFOV(Mathf.Lerp(60, 90, x));
            MyTransform.localPosition = Vector3.Lerp(currentPos, defaultWeaponPos, x);
            MyTransform.localRotation = Quaternion.Lerp(currentRot, defaultWeaponRotation, x);
        }).setOnComplete(() => scopeID = -1).uniqueId;
    }

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
