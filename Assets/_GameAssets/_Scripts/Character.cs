using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace HLProject
{
    public enum DamageType { Base, Bleed, Bullet, Explosion, Blunt, Shock }

    public class Character : CachedNetTransform
    {
        [Header("Character")]

        [SerializeField] protected float maxHealth, maxArmor;
        [SerializeField] protected float maxBleedTime, bleedThreshold, maxShockTime;
        [SerializeField][SyncVar] protected bool isInvencible;

        [SyncVar] protected bool onShock;
        [SyncVar] protected double shockTime;
        [SyncVar] protected bool isDead;
        [SyncVar] protected float suppressionAmmount;
        [SyncVar(hook = nameof(OnHealthChange))] protected float currentHealth;
        [SyncVar(hook = nameof(OnArmorChange))] protected float currentArmor;
        [SyncVar(hook = nameof(OnBleedingSet))] protected bool isBleeding;

        public bool IsDead => isDead;

        float suppressionTargetValue;
        double bleedTime, suppressionTime;

        public System.Action OnPlayerDead;

        #region Hooks

        protected void OnBleedingSet(bool oldValue, bool newValue) { }

        protected void OnHealthChange(float oldValue, float newValue)
        {
            currentHealth = Mathf.Clamp(newValue, 0, maxHealth);
        }

        protected void OnArmorChange(float oldValue, float newValue)
        {
            currentArmor = Mathf.Clamp(newValue, 0, maxArmor);
        }

        #endregion

        public override void OnStartServer() => InitCharacter();

        protected virtual void Update()
        {
            if (!isServer || isDead) return;

            if (suppressionAmmount > 0 && NetworkTime.time >= suppressionTime)
            {
                suppressionTargetValue = Mathf.Clamp01(suppressionTargetValue - Time.deltaTime * .5f);
                suppressionTime = NetworkTime.time + .2f;
            }

            suppressionAmmount = Mathf.Lerp(suppressionAmmount, suppressionTargetValue, Time.deltaTime);

            if (onShock && NetworkTime.time >= shockTime)
            {
                onShock = false;
            }

            if (!isBleeding || NetworkTime.time < bleedTime) return;

            TakeDamage(1, DamageType.Bleed);
            bleedTime = NetworkTime.time + maxBleedTime;
        }

        [Server]
        public virtual void ApplySuppression(float ammount)
        {
            suppressionAmmount = Mathf.Clamp01(suppressionAmmount + ammount);
            suppressionTargetValue = suppressionAmmount;
            suppressionTime = NetworkTime.time + .2f;
        }

        [Server]
        public virtual void TakeDamage(float ammount, DamageType damageType = DamageType.Base)
        {
            if (!isServer || isDead || isInvencible) return;

            float damageToArmor = ammount * .6f;

            switch (damageType)
            {
                case DamageType.Base:
                    damageToArmor = 0;
                    ApplySuppression(.1f);
                    break;

                case DamageType.Bleed:
                case DamageType.Explosion:
                    damageToArmor = 0;
                    break;

                case DamageType.Blunt:
                    if (!isBleeding) bleedTime = NetworkTime.time + maxBleedTime;
                    isBleeding = true;
                    ApplySuppression(.01f);
                    break;

                case DamageType.Shock:
                    onShock = true;
                    shockTime = NetworkTime.time + maxShockTime;
                    ApplySuppression(.2f);
                    break;

                default:
                    if (currentArmor <= 0 && ammount >= bleedThreshold)
                    {
                        if (!isBleeding) bleedTime = NetworkTime.time + maxBleedTime;
                        isBleeding = true;
                    }

                    ApplySuppression(.1f);
                    break;
            }

            float dmgToHealth;
            if (currentArmor > 0)
            {
                currentArmor -= damageToArmor;
                dmgToHealth = ammount - damageToArmor;
            }
            else
            {
                dmgToHealth = ammount + damageToArmor;
            }

            currentHealth -= Mathf.Clamp(dmgToHealth, 0, 9999999);
            RpcCharacterTookDamage(ammount, damageType);

            print($"Character {name} took {ammount} of {damageType} damage - DamageToArmor: {damageToArmor} - DamageToHealth: {dmgToHealth}");
            if (currentHealth <= 0) CharacterDies(damageType == DamageType.Base);
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
        protected virtual void CharacterDies(bool criticalHit)
        {
            if (!isServer || isDead || isInvencible) return;
            isBleeding = false;
            isDead = true;
            OnPlayerDead?.Invoke();
            RpcCharacterDied();
        }

        protected void InitCharacter()
        {
            currentHealth = maxHealth;
            currentArmor = maxArmor;
            isDead = false;
        }

        [ClientRpc]
        protected virtual void RpcCharacterDied()
        {
            Destroy(gameObject);
        }

        [ClientRpc]
        protected virtual void RpcCharacterTookDamage(float ammount, DamageType type)
        {
            Debug.LogFormat("Character took {0} of {1}", ammount, type);
        }

        [Server]
        public void OnBulletFlyby(Vector3 origin)
        {
            float dist = Vector3.Distance(MyTransform.position, origin);
            ApplySuppression(.2f * (dist / 1));
            Debug.LogFormat("Dist: {0} - Sup: {1}", dist, .1f * (dist / 1));
        }
    }
}
