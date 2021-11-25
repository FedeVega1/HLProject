using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Character : CachedNetTransform
{
    [SerializeField] protected float maxHealth, maxArmor;

    [SerializeField] [SyncVar] protected bool isInvencible;
    [SyncVar(hook = nameof(OnHealthChange))] protected float currentHealth;
    [SyncVar(hook = nameof(OnArmorChange))] protected float currentArmor;
    [SyncVar] protected bool isDead;

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

    [Server]
    public virtual void TakeDamage(float ammount)
    {
        if (!isServer || isDead || isInvencible) return;

        float damageToArmor = ammount * .6f;
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
