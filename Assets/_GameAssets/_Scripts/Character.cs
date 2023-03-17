using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public enum DamageType { Base, Bleed, Bullet, Explosion }

public class Character : CachedNetTransform
{
    [Header("Character")]

    [SerializeField] protected float maxHealth, maxArmor;
    [SerializeField] protected float maxBleedTime;
    [SerializeField] protected float bleedThreshold;
    [SerializeField] protected NetworkVariable<bool> isInvencible = new NetworkVariable<bool>();

    protected NetworkVariable<bool> isDead = new NetworkVariable<bool>();
    protected NetworkVariable<float> currentHealth = new NetworkVariable<float>();
    protected NetworkVariable<float> currentArmor = new NetworkVariable<float>();
    protected NetworkVariable<bool> isBleeding = new NetworkVariable<bool>();

    public bool IsDead => isDead.Value;

    double bleedTime;

    public System.Action OnPlayerDead;

    #region Hooks

    protected void OnBleedingSet(bool oldValue, bool newValue) {}

    protected void OnHealthChange(float oldValue, float newValue)
    {
        //currentHealth = Mathf.Clamp(newValue, 0, maxHealth);
    }

    protected void OnArmorChange(float oldValue, float newValue)
    {
        //currentArmor = Mathf.Clamp(newValue, 0, maxArmor);
    }

    #endregion

    protected override void OnServerSpawn()
    {
        InitCharacter_Server();
    }

    protected override void OnClientSpawn()
    {
        currentHealth.OnValueChanged += OnHealthChange;
        currentArmor.OnValueChanged += OnArmorChange;
        isBleeding.OnValueChanged += OnBleedingSet;
    }

    protected virtual void Update()
    {
        if (!IsServer || isDead.Value || !isBleeding.Value || NetTime < bleedTime) return;

        TakeDamage_Server(1, DamageType.Bleed);
        bleedTime = NetTime + maxBleedTime;
    }

    public virtual void TakeDamage_Server(float ammount, DamageType damageType = DamageType.Base)
    {
        if (!IsServer || isDead.Value || isInvencible.Value) return;

        float damageToArmor = ammount * .6f;

        switch (damageType)
        {
            case DamageType.Base:
            case DamageType.Bleed:
                damageToArmor = 0;
                break;

            default:
                if (currentArmor.Value <= 0 && ammount >= bleedThreshold)
                {
                    isBleeding.Value = true;
                    bleedTime = NetTime + maxBleedTime;
                }
                break;
        }
        
        float dmgToHealth;
        if (currentArmor.Value > 0)
        {
            currentArmor.Value -= damageToArmor;
            dmgToHealth = ammount - damageToArmor;
        }
        else
        {
            dmgToHealth = ammount + damageToArmor;
        }
        
        currentHealth.Value -= Mathf.Clamp(dmgToHealth, 0, 9999999);

        print($"Character {name} took {ammount} of {damageType} damage - DamageToArmor: {damageToArmor} - DamageToHealth: {dmgToHealth}");
        if (currentHealth.Value <= 0) CharacterDies_Server(damageType == DamageType.Base);
    }

    public virtual void TakeHealth_Server(float ammount)
    {
        if (!IsServer || isDead.Value) return;
        currentHealth.Value += ammount;
    }

    public virtual void TakeArmor_Server(float ammount)
    {
        if (!IsServer || isDead.Value) return;
        currentArmor.Value += ammount;
    }

    protected virtual void CharacterDies_Server(bool criticalHit)
    {
        if (!IsServer || isDead.Value || isInvencible.Value) return;
        isBleeding.Value = false;
        isDead.Value = true;
        OnPlayerDead?.Invoke();
        CharacterDied_ClientRpc();
    }

    protected void InitCharacter_Server()
    {
        currentHealth.Value = maxHealth;
        currentArmor.Value = maxArmor;
        isDead.Value = false;
    }

    [ClientRpc]
    protected virtual void CharacterDied_ClientRpc()
    {
        Destroy(gameObject);
    }
}
