using UnityEditor;
using UnityEngine;
using HLProject.Weapons;

namespace HLProject.Scriptables
{
    [CustomEditor(typeof(WeaponData))]
    public class WeaponDataEditor : Editor
    {
        SerializedProperty weaponName, clientPrefab, propPrefab, bulletData, maxBulletSpread, weaponWeight, meleeDamage,
            weaponType, weaponAnimsTiming, bulletsPerMag, mags, meleeDamageType, pelletsPerShot, recoilPatternX, 
            recoilPatternY, recoilPatternZ, singleRecoilShoot, avaibleWeaponFireModes, alternateWeaponMode;

        bool hasAltMode;

        void OnEnable()
        {
            weaponName = serializedObject.FindProperty("weaponName");
            clientPrefab = serializedObject.FindProperty("clientPrefab");
            propPrefab = serializedObject.FindProperty("propPrefab");
            bulletData = serializedObject.FindProperty("bulletData");
            maxBulletSpread = serializedObject.FindProperty("maxBulletSpread");
            weaponWeight = serializedObject.FindProperty("weaponWeight");
            meleeDamage = serializedObject.FindProperty("meleeDamage");
            weaponType = serializedObject.FindProperty("weaponType");
            weaponAnimsTiming = serializedObject.FindProperty("weaponAnimsTiming");
            bulletsPerMag = serializedObject.FindProperty("bulletsPerMag");
            mags = serializedObject.FindProperty("mags");
            pelletsPerShot = serializedObject.FindProperty("pelletsPerShot");
            meleeDamageType = serializedObject.FindProperty("meleeDamageType");
            recoilPatternX = serializedObject.FindProperty("recoilPatternX");
            recoilPatternY = serializedObject.FindProperty("recoilPatternY");
            recoilPatternZ = serializedObject.FindProperty("recoilPatternZ");
            singleRecoilShoot = serializedObject.FindProperty("singleRecoilShoot");
            avaibleWeaponFireModes = serializedObject.FindProperty("avaibleWeaponFireModes");
            alternateWeaponMode = serializedObject.FindProperty("alternateWeaponMode");

            hasAltMode = PlayerPrefs.GetInt($"Editor_WeaponData_{serializedObject.targetObject.name}_HasAltMode") == 1;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(weaponName);
            EditorGUILayout.PropertyField(clientPrefab);
            EditorGUILayout.PropertyField(propPrefab);
            EditorGUILayout.PropertyField(weaponWeight);

            hasAltMode = EditorGUILayout.Toggle("Is this data from an Alt Mode?", hasAltMode);
            PlayerPrefs.SetInt($"Editor_WeaponData_{serializedObject.targetObject.name}_HasAltMode", hasAltMode ? 1 : 0);
            if (!hasAltMode) EditorGUILayout.PropertyField(alternateWeaponMode);

            EditorGUILayout.PropertyField(weaponAnimsTiming);

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            weaponType.enumValueIndex = EditorGUILayout.Popup("Weapon Type", weaponType.enumValueIndex, weaponType.enumNames);

            switch ((WeaponType) weaponType.enumValueIndex)
            {
                case WeaponType.Melee:
                    EditorGUILayout.PropertyField(meleeDamage);
                    EditorGUILayout.PropertyField(meleeDamageType);
                    break;

                default:
                    EditorGUILayout.PropertyField(bulletData);
                    EditorGUILayout.PropertyField(maxBulletSpread);
                    EditorGUILayout.PropertyField(bulletsPerMag);
                    EditorGUILayout.PropertyField(mags);
                    EditorGUILayout.PropertyField(pelletsPerShot);

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("Recoil Pattern");

                    CheckAnimationCurve(ref recoilPatternX);
                    CheckAnimationCurve(ref recoilPatternY);
                    CheckAnimationCurve(ref recoilPatternZ);

                    recoilPatternX.animationCurveValue = EditorGUILayout.CurveField(recoilPatternX.animationCurveValue);
                    recoilPatternY.animationCurveValue = EditorGUILayout.CurveField(recoilPatternY.animationCurveValue);
                    recoilPatternZ.animationCurveValue = EditorGUILayout.CurveField(recoilPatternZ.animationCurveValue);
                    GUILayout.EndHorizontal();

                    EditorGUILayout.PropertyField(singleRecoilShoot);
                    EditorGUILayout.PropertyField(avaibleWeaponFireModes);
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        void CheckAnimationCurve(ref SerializedProperty property)
        {
            if (property.animationCurveValue == null) return;
            if (property.animationCurveValue.length == 0)
            {
                AnimationCurve newCurve = new AnimationCurve(new Keyframe[2] { new Keyframe(0, 0), new Keyframe(1, 0) });
                property.animationCurveValue = newCurve;
            }
        }
    }
}
