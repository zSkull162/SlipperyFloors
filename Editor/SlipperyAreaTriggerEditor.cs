#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UnityEngine;
using zSkull162.Editor.Inspector;
using zSkull162.Editor.Inspector.Themes;

namespace zSkull162.SlipperyFloor
{
    [CustomEditor(typeof(SlipperyAreaTrigger))]
    public class SlipperyAreaTriggerEditor : UnityEditor.Editor
    {
        SerializedProperty controller;
        SerializedProperty isActive;

        void OnEnable() {
            controller = serializedObject.FindProperty("controller");
            isActive = serializedObject.FindProperty("isActive");
            
            SlipperyAreaTrigger script = (SlipperyAreaTrigger)target;
            script._ValidateIsTrigger();
        }

        public override void OnInspectorGUI() {
            UdonSharpEditor.UdonSharpGUI.DrawProgramSource(target);
            
            serializedObject.Update();
            
            EditorGUILayout.Space();
            using (new InspectorGUI.VerticalScope(ThemeColor.Col1)) {
                EditorGUILayout.PropertyField(controller);
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(isActive);
            }
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif