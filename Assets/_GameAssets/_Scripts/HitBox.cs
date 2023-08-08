using System.Collections.Generic;
using UnityEngine;

namespace HLProject.Characters
{
    public enum HitBoxType { Generic, Head, Torso, Leg, Arm }

    public class HitBox : CachedTransform
    {
        static readonly Dictionary<HitBoxType, float> damageMultTable = new Dictionary<HitBoxType, float>()
        {
            { HitBoxType.Head, 2 },
            { HitBoxType.Torso, 1 },
            { HitBoxType.Arm, .6f },
            { HitBoxType.Leg, .5f },
            { HitBoxType.Generic, 1 },
        };

        [SerializeField] HitBoxType type;

        public Transform CharacterTransform => characterScript.MyTransform;
        public bool CharacterIsDead => characterScript.IsDead;

        Character characterScript;

        void Awake()
        {
            characterScript = MyTransform.root.GetComponent<Character>();
        }

        public void TakeDamage(float ammount, DamageType damageType = DamageType.Base)
        {
            if (CharacterIsDead) return;
            characterScript.TakeDamage(ammount * damageMultTable[type], damageType);
        }

        public void OnBulletFlyby(Vector3 origin) => characterScript.OnBulletFlyby(origin);

        public Character GetCharacterComponent() => characterScript;
    }
}
