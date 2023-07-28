using UnityEngine;

namespace HLProject.Characters
{
    public enum HitBoxType { Generic, Head, Torso, Leg, Arm }

    public class HitBox : CachedTransform
    {
        [SerializeField] HitBoxType type;
        [SerializeField] Character characterScript;

        public Character GetCharacterScript() => characterScript;
    }
}
