using UnityEditor;
using UnityEngine;

namespace HLProject
{
    [CustomEditor(typeof(BulletData))]
    public class BulletDataEditor : Editor
    {
        SerializedProperty bulletPrefab, type, physType, damageType, initialSpeed, damage, maxTravelDistance,
            radious, timeToExplode, fallOff, canExplode;

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
            canExplode = serializedObject.FindProperty("canExplode");
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

            canExplode.boolValue = EditorGUILayout.Toggle("Is Explosive?", canExplode.boolValue);
            PlayerPrefs.SetInt($"Editor_BulletData_{serializedObject.targetObject.name}_IsExplosive", canExplode.boolValue ? 1 : 0);

            if (canExplode.boolValue)
            {
                EditorGUILayout.PropertyField(radious);
                string label = (BulletPhysicsType) physType.enumValueIndex == BulletPhysicsType.FireBounce ? "Bounce Count" : "Time to Explode";
                timeToExplode.floatValue = EditorGUILayout.FloatField(label, timeToExplode.floatValue);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
