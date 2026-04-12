
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using zSkull162.Editor.Tools;
using zSkull162.Runtime.Logging;

namespace zSkull162.SlipperyFloor
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None), RequireComponent(typeof(Collider))]
    public class SlipperyAreaTrigger : UdonSharpBehaviour
    {
        [SerializeField] private SlipperyFloorController controller;
        [Tooltip("Will be automatically set based on the active state of this gameobject, but is exposed in case you want to modify it by other scripts")]
        [SerializeField] private bool isActive = true;
        private bool inArea;
        
        public bool InArea => inArea;
        public bool IsActive {
            set => isActive = value;
            get => isActive;
        }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        void OnValidate() {
            if (controller == null) EditorRefUtils.AssignFirst(ref controller);
            _ValidateIsTrigger();
        }
        
        public void _ValidateIsTrigger() {
            Collider col = GetComponent<Collider>();
            if (col.isTrigger) return;
            col.isTrigger = true;
            if (PrefabUtility.IsPartOfPrefabInstance(col)) PrefabUtility.RecordPrefabInstancePropertyModifications(col);
        }
#endif

        void OnEnable() => isActive = true;
        void OnDisable() => isActive = false;

        public override void OnPlayerTriggerEnter(VRCPlayerApi player) {
            if (controller.Logs) zLogger.Log(name, $"[OnPlayerTriggerEnter] {player.displayName} entered the trigger", LogColor.LightBlue);
            if (!player.isLocal || !isActive) return;
            inArea = true;
            controller._OnTriggerEnter();
        }
        
        override public void OnPlayerTriggerExit(VRCPlayerApi player) {
            if (controller.Logs) zLogger.Log(name, $"[OnPlayerTriggerExit] {player.displayName} left the trigger", LogColor.LightBlue);
            if (!player.isLocal || !isActive) return;
            inArea = false;
            controller._OnTriggerExit();
        }
    }
}