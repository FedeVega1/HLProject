using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

namespace HLProject
{
    public enum ClientHandType { Citizen, Rebel, Metrocop, CombineSoldier, CombineElite }

    public class PlayerClientHands : CachedTransform
    {
        [SerializeField] Transform handRootBone;

        Dictionary<string, PositionConstraint> _HandPosConstrains;
        Dictionary<string, PositionConstraint> HandPosConstrains
        {
            get
            {
                if (_HandPosConstrains == null) SetupConstrains();
                return _HandPosConstrains;
            }
        }

        Dictionary<string, RotationConstraint> _HandRotConstrains;
        Dictionary<string, RotationConstraint> HandRotConstrains
        {
            get
            {
                if (_HandRotConstrains == null) SetupConstrains();
                return _HandRotConstrains;
            }
        }

        public void GetWeaponHandBones(Transform weaponRootBone, Transform wRoot)
        {
            if (weaponRootBone == null) return;

            CleanConstrainsSources();
            Transform[] weaponBones = weaponRootBone.GetComponentsInChildren<Transform>(true);

            int size = weaponBones.Length;
            for (int i = 0; i < size; i++)
            {
                if (!HandPosConstrains.ContainsKey(weaponBones[i].name)) continue;
                HandPosConstrains[weaponBones[i].name].AddSource(new ConstraintSource { sourceTransform = weaponBones[i], weight = 1 });
                HandRotConstrains[weaponBones[i].name].AddSource(new ConstraintSource { sourceTransform = weaponBones[i], weight = 1 });

                HandPosConstrains[weaponBones[i].name].constraintActive = true;
                HandRotConstrains[weaponBones[i].name].constraintActive = true;

                Debug.Log(weaponBones[i].name);
            }
        }

        void SetupConstrains()
        {
            _HandPosConstrains = new Dictionary<string, PositionConstraint>();
            _HandRotConstrains = new Dictionary<string, RotationConstraint>();
            Transform[] bones = handRootBone.GetComponentsInChildren<Transform>(true);

            int size = bones.Length;
            for (int i = 0; i < size; i++)
            {
                _HandPosConstrains.Add(bones[i].name, bones[i].gameObject.AddComponent<PositionConstraint>());
                _HandRotConstrains.Add(bones[i].name, bones[i].gameObject.AddComponent<RotationConstraint>());
            }
        }

        void CleanConstrainsSources()
        {
            foreach (string key in HandPosConstrains.Keys)
            {
                if (HandPosConstrains[key].sourceCount > 0) HandPosConstrains[key].RemoveSource(0);
                if (HandRotConstrains[key].sourceCount > 0) HandRotConstrains[key].RemoveSource(0);
            }
        }
    }
}
