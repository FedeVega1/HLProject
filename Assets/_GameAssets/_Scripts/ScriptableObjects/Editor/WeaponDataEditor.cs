using UnityEditor;

namespace HLProject
{
    [CustomEditor(typeof(WeaponData))]
    public class WeaponDataEditor : Editor
    {
        SerializedProperty weaponName, clientPrefab, propPrefab, bulletData, maxBulletSpread, weaponWeight, meleeDamage,
            weaponType, weaponAnimsTiming, bulletsPerMag, mags, meleeDamageType, pelletsPerShot;

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
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(weaponName);
            EditorGUILayout.PropertyField(clientPrefab);
            EditorGUILayout.PropertyField(propPrefab);
            EditorGUILayout.PropertyField(weaponWeight);
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
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
