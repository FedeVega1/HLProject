using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetMeleeSystem : MonoBehaviour
{
    [SerializeField] Transform weaponPivot;

    bool onAttackMode;
    GameObject currentDummyWeapon;
    Bounds meleeHitBox;
    Vector3 referenceOffset;
    Transform meleeHitPoint;
    Animator netAnimator;

    public void ChangeCurrentWeapon(WeaponData data)
    {
        if (data.weaponType != WeaponType.Melee) return;
        if (currentDummyWeapon != null) Destroy(currentDummyWeapon);

        currentDummyWeapon = Instantiate(data.clientPrefab, weaponPivot.position, weaponPivot.rotation, weaponPivot);
        BaseClientWeapon clientWeapon = currentDummyWeapon.GetComponent<BaseClientWeapon>();

        if (clientWeapon == null) return;
        clientWeapon.Init(false, null, null, null);
 
        netAnimator = clientWeapon.GetCurrentViewmodelAnimator();
        meleeHitPoint = clientWeapon.GetMeleeHitPoint();

        Bounds boundRef = clientWeapon.GetMeleeHitBox();
        referenceOffset = boundRef.center;
        meleeHitBox = new Bounds(referenceOffset + meleeHitPoint.position, boundRef.size);
    }

    void LateUpdate()
    {
        if (!onAttackMode) return;
        meleeHitBox.center = referenceOffset + meleeHitPoint.position;
    }

    public void ToggleMeleeMode(bool toggle)
    {
        currentDummyWeapon.SetActive(toggle);
        netAnimator.SetTrigger("Idle");
    }

    public void Attack()
    {
        netAnimator.SetTrigger("Shoot");
        onAttackMode = true;
    }
}
