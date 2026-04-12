
using UdonSharp;
using UnityEngine;
using UnityEditor;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using zSkull162.Editor.Tools;
using zSkull162.Runtime.Logging;

namespace zSkull162.SlipperyFloor
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SlipperyFloorController : UdonSharpBehaviour
    {
        #region Fields
        [Tooltip("The object with the collider that has a slippery physics material, which will be used to control the player")]
        [SerializeField] private GameObject playerCollider;
        [Tooltip("The triggers that define slippery areas. These are automatically assigned if they exist in the scene")]
        [SerializeField] SlipperyAreaTrigger[] areaTriggers;
        [Tooltip("A multiplier applied to the player's movement speed when on a slippery floor")]
        [SerializeField] private float movementSpeedMultiplier = 0.6f;
        [Tooltip("The maximum speed the player can reach (when walking) on the slippery floor. Set to zero to disable max speed")]
        [SerializeField] private float maxSpeed = 3.75f;
        [Tooltip("If true, the max speed will automatically be set to the player's run speed")]
        [SerializeField] private bool clampMaxSpeedToRunSpeed = true;
        [Tooltip("A multiplier applied to the player's jump height while in a slippery area")]
        [SerializeField] private float jumpMultiplier = 1f;
        [Tooltip("A multiplier applied to the player's movement speed when jumping while in a slippery area\nA good value to keep speed consistent is 1.1, and a good value to allow players to accelerate a little is 1.25")]
        [SerializeField] private float jumpMovementMultiplier = 1.25f;
        [Tooltip("How often (in seconds) to check if the player is grounded after leaving a slippery area, to prevent the player from getting stopped as soon as they leave the area")]
        [SerializeField] private float groundedCheckRate = 0.075f;
        [Tooltip("Enables logs in the console for debugging")]
        [SerializeField] private bool logs;
        
        [SerializeField, HideInInspector] private Rigidbody colliderRB;
        [SerializeField, HideInInspector] bool showTriggers;
        private VRCPlayerApi localPlayer;
        private float jumpImpulse;
        private bool active;

        public bool Logs => logs;
        public bool Active {
            set => active = value;
            get => active;
        }
        private bool InSlipperyArea {
            get {
                foreach (var trigger in areaTriggers) {
                    if (trigger == null) continue;
                    if (trigger.InArea) return true;
                }
                return false;
            }
        }

        private Vector3 PlayerPos => localPlayer.GetPosition();
        private Vector3 PlayerVelocity => localPlayer.GetVelocity();
        private bool IsGrounded => localPlayer.IsPlayerGrounded();

    #if UNITY_EDITOR && !COMPILER_UDONSHARP
        void OnValidate() {
            _AssignTriggers();
            if (playerCollider != null) EditorRefUtils.AssignComponent(ref colliderRB, playerCollider);
            
            if (colliderRB == null) return; // Make sure the rigidbody's settings are set-up properly for the slippery floor system
            if (!colliderRB.freezeRotation) colliderRB.freezeRotation = true;
            if (colliderRB.isKinematic) colliderRB.isKinematic = false;
            if (!colliderRB.useGravity) colliderRB.useGravity = true;
            if (PrefabUtility.IsPartOfPrefabInstance(colliderRB)) PrefabUtility.RecordPrefabInstancePropertyModifications(colliderRB);
        }
        
        public void _AssignTriggers() => EditorRefUtils.AssignAll(ref areaTriggers);
    #endif
        #endregion
        
        void Log(string name, string message, LogColor color) {
            if (logs) zLogger.Log(name, message, color);
        }
        
        void Start() {
            localPlayer = Networking.LocalPlayer;
            movementSpeedMultiplier /= 100f; // Dividing by 100 to make the inspector value more intuitive. eg 0.6 instead of 0.006
        }

        public override void OnPlayerRestored(VRCPlayerApi player) { // Getting the jump impulse/run speed from OnPlayerRestored to be safe against race conditions
            if (!player.isLocal) return;
            jumpImpulse = Networking.LocalPlayer.GetJumpImpulse();
            if (clampMaxSpeedToRunSpeed) maxSpeed = Networking.LocalPlayer.GetRunSpeed();
            
            Log(name, $"[OnPlayerRestored] Jump Impulse: {jumpImpulse}, Run Speed: {maxSpeed}", LogColor.Magenta);
            if (maxSpeed <= 0f) maxSpeed = 999f; // If the max speed is zero, set it high to act like there's no max speed
        }


        #region Input Events
        public void _OnTriggerEnter() {
            if (Active) return;
            playerCollider.SetActive(true);
            ColliderToPlayer();
            Active = true;
        }
        
        public void _OnTriggerExit() {
            if (!InSlipperyArea) _DisableWhenGrounded();
        }

        public override void OnPlayerRespawn(VRCPlayerApi player) {
            if (!player.isLocal) return;
            Log(name, "[OnPlayerRespawn] Respawn detected", LogColor.LightBlue);
            
            if (InSlipperyArea) return;
            playerCollider.SetActive(false);
            Active = false;
        }

        public override void InputJump(bool value, UdonInputEventArgs args) {
            if (!Active || !value || jumpImpulse <= 0f || !IsGrounded || !InSlipperyArea) return;
            Log(name, $"[InputJump] Detected", LogColor.LightBlue);
            
            colliderRB.velocity = new Vector3(
                colliderRB.velocity.x * jumpMovementMultiplier,
                jumpImpulse * jumpMultiplier,
                colliderRB.velocity.z * jumpMovementMultiplier
            );
        }
        #endregion

        private void ColliderToPlayer() {
            if (!InSlipperyArea) return;
            Log(name, $"[ColliderToPlayer] Moving collider from {playerCollider.transform.position} to {PlayerPos}", LogColor.Blue);
            
            playerCollider.transform.position = PlayerPos;
            colliderRB.velocity = PlayerVelocity;
        }
        
        public void _DisableWhenGrounded() {
            if (!IsGrounded) {
                Log(name, "[_DisableWhenGrounded] Waiting...", LogColor.Blue);
                SendCustomEventDelayedSeconds(nameof(_DisableWhenGrounded), groundedCheckRate);
                return;
            }
            Log(name, "[_DisableWhenGrounded] Disabling", LogColor.Blue);
            playerCollider.SetActive(false);
            Active = false;
        }

        #region Movement Logic
        void FixedUpdate() {
            if (!Active) return;
            
            if (colliderRB.velocity.magnitude <= maxSpeed) colliderRB.velocity = PlayerVelocity * movementSpeedMultiplier + colliderRB.velocity;
            StableTeleportTo(playerCollider.transform.position);
        }
        
        private void StableTeleportTo(Vector3 destinationPos) {
            var origin = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin);
            Vector3 distToOrigin = origin.position - PlayerPos;

            localPlayer.TeleportTo(destinationPos + distToOrigin, origin.rotation, VRC_SceneDescriptor.SpawnOrientation.AlignRoomWithSpawnPoint, true);
        }
        #endregion
    }
}