#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UnityEngine;
using zSkull162.Editor.Inspector;
using zSkull162.Editor.Inspector.Themes;

namespace zSkull162.SlipperyFloor
{
    [CustomEditor(typeof(SlipperyFloorController))]
    public class SlipperyFloorControllerEditor : UnityEditor.Editor
    {
        SerializedProperty playerCollider;
        SerializedProperty areaTriggers;
        SerializedProperty movementSpeedMultiplier;
        SerializedProperty maxSpeed;
        SerializedProperty clampMaxSpeedToRunSpeed;
        SerializedProperty jumpMultiplier;
        SerializedProperty jumpMovementMultiplier;
        SerializedProperty groundedCheckRate;
        SerializedProperty logs;
        
        SerializedProperty showTriggers;

        void OnEnable() {
            playerCollider = serializedObject.FindProperty("playerCollider");
            areaTriggers = serializedObject.FindProperty("areaTriggers");
            movementSpeedMultiplier = serializedObject.FindProperty("movementSpeedMultiplier");
            maxSpeed = serializedObject.FindProperty("maxSpeed");
            clampMaxSpeedToRunSpeed = serializedObject.FindProperty("clampMaxSpeedToRunSpeed");
            jumpMultiplier = serializedObject.FindProperty("jumpMultiplier");
            jumpMovementMultiplier = serializedObject.FindProperty("jumpMovementMultiplier");
            groundedCheckRate = serializedObject.FindProperty("groundedCheckRate");
            logs = serializedObject.FindProperty("logs");
            
            showTriggers = serializedObject.FindProperty("showTriggers");
            
            SlipperyFloorController controller = (SlipperyFloorController) target;
            controller._AssignTriggers();
        }

        public override void OnInspectorGUI() {
            UdonSharpEditor.UdonSharpGUI.DrawProgramSource(target);
            
            InspectorGUI.TitleLabel("Slippery Floors v2");
            
            serializedObject.Update();
            
            using (new InspectorGUI.VerticalScope(ThemeColor.Col2)) {
                InspectorGUI.SectionLabel(ThemeColor.Col2, "Scene References");
                EditorGUILayout.PropertyField(playerCollider);
                showTriggers.boolValue = InspectorGUI.ArrayField(areaTriggers, showTriggers.boolValue);
            }
            EditorGUILayout.Space();
            using (new InspectorGUI.VerticalScope(ThemeColor.Col3)) {
                InspectorGUI.SectionLabel(ThemeColor.Col3, "Movement");
                EditorGUILayout.PropertyField(movementSpeedMultiplier);
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(clampMaxSpeedToRunSpeed);
                if (!clampMaxSpeedToRunSpeed.boolValue) EditorGUILayout.PropertyField(maxSpeed);
            }
            EditorGUILayout.Space();
            using (new InspectorGUI.VerticalScope(ThemeColor.Col4)) {
                InspectorGUI.SectionLabel(ThemeColor.Col4, "Jumping");
                EditorGUILayout.PropertyField(jumpMultiplier);
                EditorGUILayout.PropertyField(jumpMovementMultiplier);
            }
            EditorGUILayout.Space();
            using (new InspectorGUI.VerticalScope(ThemeColor.Col5)) {
                InspectorGUI.SectionLabel(ThemeColor.Col5, "Other");
                EditorGUILayout.PropertyField(groundedCheckRate);
                EditorGUILayout.PropertyField(logs);
            }
            
            // Clamp values to zero in-inspector instead of in the script
            if (movementSpeedMultiplier.floatValue < 0f) movementSpeedMultiplier.floatValue = 0f;
            if (maxSpeed.floatValue < 0f) maxSpeed.floatValue = 0f;
            if (jumpMultiplier.floatValue < 0f) jumpMultiplier.floatValue = 0f;
            if (jumpMovementMultiplier.floatValue < 0f) jumpMovementMultiplier.floatValue = 0f;
            if (groundedCheckRate.floatValue < 0f) groundedCheckRate.floatValue = 0f;

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif