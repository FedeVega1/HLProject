using UnityEditor;
using UnityEngine;

namespace HLProject
{
    [CustomEditor(typeof(BulletData))]
    public class BulletDataEditor : Editor
    {
        SerializedProperty bulletPrefab, type, physType, damageType, initialSpeed, damage, maxTravelDistance,
            radious, timeToExplode, fallOff;

        bool isExplosive;

        void OnEnable()
        {
            bulletPrefab = serializedObject.FindProperty("bulletPrefab");
            type = serializedObject.FindProperty("type");
            physType = serializedObject.FindProperty("physType");
            damageType = serializedObject.FindProperty("damageType");
            initialSpeed = serializedObject.FindProperty("initialSpeed");
            damage = serializedObject.FindProperty("damage");
            maxTravelDistance = serializedObject.FindProperty("maxTravelDistance");
            radious = serializedObject.FindProperty("radius");
            timeToExplode = serializedObject.FindProperty("timeToExplode");
            fallOff = serializedObject.FindProperty("fallOff");

            isExplosive = PlayerPrefs.GetInt($"Editor_BulletData_{serializedObject.targetObject.name}_IsExplosive") == 1;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(bulletPrefab);
            EditorGUILayout.PropertyField(type);

            bool useThrowText = false;
            switch ((BulletType) type.enumValueIndex)
            {
                case BulletType.RayCast:
                    EditorGUILayout.PropertyField(maxTravelDistance);
                    EditorGUILayout.PropertyField(fallOff);
                    break;

                case BulletType.Physics:
                    EditorGUILayout.PropertyField(physType);
                    useThrowText = physType.enumValueIndex == 0;
                    break;
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            initialSpeed.floatValue = EditorGUILayout.FloatField(useThrowText ? "Throw Force" : "Initial Speed", initialSpeed.floatValue);
            EditorGUILayout.PropertyField(damageType);
            EditorGUILayout.PropertyField(damage);

            isExplosive = EditorGUILayout.Toggle("Is Explosive?", isExplosive);
            PlayerPrefs.SetInt($"Editor_BulletData_{serializedObject.targetObject.name}_IsExplosive", isExplosive ? 1 : 0);

            if (isExplosive)
            {
                EditorGUILayout.PropertyField(radious);
                EditorGUILayout.PropertyField(timeToExplode);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
