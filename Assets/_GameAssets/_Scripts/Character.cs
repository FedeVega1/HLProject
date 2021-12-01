using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public enum DamageType { Base, Bleed }

public class Character : CachedNetTransform
{
    [Header("Character")]

    [SerializeField] protected float maxHealth, maxArmor;
    [SerializeField] protected float maxBleedTime;
    [SerializeField] protected float bleedThreshold;
    [SerializeField] [SyncVar] protected bool isInvencible;

    [SyncVar] protected bool isDead;
    [SyncVar(hook = nameof(OnHealthChange))] protected float currentHealth;
    [SyncVar(hook = nameof(OnArmorChange))] protected float currentArmor;
    [SyncVar(hook = nameof(OnBleedingSet))] protected bool isBleeding;

    public bool IsDead => isDead;

    double bleedTime;

    #region Hooks

    protected void OnBleedingSet(bool oldValue, bool newValue) {}

    protected void OnHealthChange(float oldValue, float newValue)
    {
        currentHealth = Mathf.Clamp(newValue, 0, maxHealth);
    }

    protected void OnArmorChange(float oldValue, float newValue)
    {
        currentArmor = Mathf.Clamp(newValue, 0, maxArmor);
    }

    public override void OnStartServer()
    {
        currentHealth = maxHealth;
        currentArmor = maxArmor;
        isDead = false;
    }

    #endregion

    protected virtual void Update()
    {
        if (!isServer || !isBleeding || NetworkTime.time < bleedTime) return;

        TakeDamage(1, DamageType.Bleed);
        bleedTime = NetworkTime.time + maxBleedTime;
    }

    [Server]
    public virtual void TakeDamage(float ammount, DamageType damageType = DamageType.Base)
    {
        if (!isServer || isDead || isInvencible) return;

        float damageToArmor = ammount * .6f;

        switch (damageType)
        {
            case DamageType.Base:
            case DamageType.Bleed:
                damageToArmor = 0;
                break;

            default:
                if (ammount >= bleedThreshold)
                {
                    isBleeding = true;
                    bleedTime = NetworkTime.time + maxBleedTime;
                }
                break;
        }

        currentArmor -= damageToArmor;
        currentHealth -= Mathf.Clamp(ammount - damageToArmor, 0, 9999999);

        if (currentHealth <= 0) CharacterDies();
    }

    [Server]
    public virtual void TakeHealth(float ammount)
    {
        if (!isServer || isDead) return;
        currentHealth += ammount;
    }

    [Server]
    public virtual void TakeArmor(float ammount)
    {
        if (!isServer || isDead) return;
        currentArmor += ammount;
    }

    [Server]
    protected virtual void CharacterDies()
    {
        if (!isServer || isInvencible) return;
        isDead = true;
        RpcCharacterDied();
    }

    [ClientRpc]
    protected virtual void RpcCharacterDied()
    {
        Destroy(gameObject);
    }
}
