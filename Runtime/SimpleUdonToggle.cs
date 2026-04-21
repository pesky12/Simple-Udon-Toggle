
using System;
using System.Reflection;
using UdonSharp;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
#endif
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VRC.SDK3.Persistence;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

public enum NetworkMode
{
    None,
    OwnerOnly,
    Synced
}

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class SimpleUdonToggle : UdonSharpBehaviour
{
    #region Configuration Fields
    // --- Configuration ---
    [Header("Identification")]
    [Tooltip("Unique name for this toggle - used to auto-assign UI toggles with matching SimpleToggleMarker components")]
    [SerializeField] public string toggleName;
    
    [Header("Initial")]
    [SerializeField] private bool defaultOn = false;
    [SerializeField] private NetworkMode networkMode = NetworkMode.None;
    [Tooltip("Save toggle state locally using VRC Persistence. Only works with NetworkMode.None or OwnerOnly - NOT compatible with Synced mode.")]
    [SerializeField] private bool persistLocally = false;
    [Tooltip("Unique key for persistence. Required if persistLocally is enabled.")]
    [SerializeField] private string persistenceKey = "";

    [Header("Click / Trigger")]
    [SerializeField] private bool allowInteract = true; // Interact() click
    [SerializeField] private bool toggleOnTriggerEnter = false; // Toggle when local player enters trigger (local only)
    [SerializeField] private bool toggleOnTriggerExit = false; // Toggle on exit

    [Header("UI")]
    [SerializeField] public Toggle[] uiToggles;
    [SerializeField] private bool updateUITogglesOnStart = true;
    private bool _suppressUiCallback = false;

    [Header("GameObjects (per-item desired states)")]
    [SerializeField] private GameObject[] targetGameObjects;
    [SerializeField] private bool[] gameObjectStateWhenOn;   // if isOn -> setActive = value
    [SerializeField] private bool[] gameObjectStateWhenOff;  // if !isOn -> setActive = value

    [Header("Components (enable/disable)")]
    [Tooltip("Components to enable/disable. Note: UdonSharpBehaviour targets should use the Udon Targets section below instead.")]
    [SerializeField] private Behaviour[] targetBehaviours;
    [SerializeField] private bool[] behaviourStateWhenOn;
    [SerializeField] private bool[] behaviourStateWhenOff;

    [Header("Udon Targets (send events)")]
    [SerializeField] private UdonBehaviour[] udonTargets;
    [SerializeField] private string[] udonEventWhenOn;   // Event name to SendCustomEvent to target when toggle becomes ON
    [SerializeField] private string[] udonEventWhenOff;  // Event name to SendCustomEvent to target when toggle becomes OFF
    
    [Header("Other Toggles")]
    [SerializeField] private SimpleUdonToggle[] otherTogglesToSync; // Other toggles to set to the same state
    [SerializeField] private bool[] otherTogglesStateWhenOn;     // true = set to ON, false = set to OFF
    [SerializeField] private bool[] otherTogglesStateWhenOff;
    #endregion

    #region State Variables
    // --- Synced state ---
    [UdonSynced(UdonSyncMode.None)] public bool syncedIsOn;

    // Local cached state
    private bool isOn;
    private string _computedPersistenceKey;
    private bool _restored = false;
    private bool _syncReceived = false; // Guards against race condition where Start() runs before OnDeserialization()
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        // Runtime safeguard: Persistence and Synced mode are mutually exclusive
        if (persistLocally && networkMode == NetworkMode.Synced)
        {
            Debug.LogError("[SimpleUdonToggle] Persistence cannot be used with Synced network mode. Persistence is for local-only toggles. Disabling persistLocally.", this);
            persistLocally = false;
        }
        
        // Compute persistence key
        if (persistLocally)
        {
            if (string.IsNullOrEmpty(persistenceKey))
            {
                Debug.LogError("[SimpleUdonToggle] Persistence is enabled, but no Persistence Key is set. State will not be saved. Please provide a unique key.", this);
                persistLocally = false;
            }
            else
            {
                _computedPersistenceKey = persistenceKey;
            }
        }
    
        // Determine initial state
        if (networkMode == NetworkMode.Synced)
        {
            if (Networking.IsOwner(gameObject))
            {
                // We are the owner, assume 'defaultOn' is the intended start state
                syncedIsOn = defaultOn;
                isOn = syncedIsOn;
                _syncReceived = true;
                RequestSerialization(); 
            }
            else
            {
                // Not owner, defer state application until OnDeserialization() provides synced data.
                // The _syncReceived flag prevents ApplyState from running with stale data.
                // We set isOn to defaultOn as a fallback, but won't apply until sync arrives.
                isOn = defaultOn;
            }
        }
        else
        {
            // Simple local or owner-only mode
            isOn = defaultOn;
        }
    
        // Only apply state immediately if we're not waiting for sync
        if (!persistLocally && (networkMode != NetworkMode.Synced || _syncReceived))
        {
            ApplyState(isOn);
        }
    
        #if UNITY_EDITOR
        Debug.Log($"[SimpleUdonToggle] Initialized. Owner:{Networking.IsOwner(gameObject)} NetMode:{networkMode} Default:{defaultOn} -> IsOn:{isOn}");
        #endif
    }
    #endregion

#if UNITY_EDITOR
    #region Editor Validation
    private void OnValidate()
    {
        ValidateArrayLengths();
        ValidatePersistenceNetworkMode();
    }

    private void ValidatePersistenceNetworkMode()
    {
        // Persistence and network sync are mutually exclusive
        // Persistence is for local-only toggles; synced state comes from network
        if (persistLocally && networkMode == NetworkMode.Synced)
        {
            Debug.LogWarning($"[SimpleUdonToggle] '{gameObject.name}': Persistence cannot be used with Synced network mode. Persistence is for local-only toggles. Disabling persistLocally.", this);
            persistLocally = false;
        }
    }

    private void ValidateArrayLengths()
    {
        // Validate GameObject arrays
        if (targetGameObjects != null)
        {
            int targetCount = targetGameObjects.Length;
            if (gameObjectStateWhenOn != null && gameObjectStateWhenOn.Length != targetCount)
            {
                Debug.LogWarning($"[SimpleUdonToggle] '{gameObject.name}': gameObjectStateWhenOn array length ({gameObjectStateWhenOn.Length}) doesn't match targetGameObjects ({targetCount}).", this);
            }
            if (gameObjectStateWhenOff != null && gameObjectStateWhenOff.Length != targetCount)
            {
                Debug.LogWarning($"[SimpleUdonToggle] '{gameObject.name}': gameObjectStateWhenOff array length ({gameObjectStateWhenOff.Length}) doesn't match targetGameObjects ({targetCount}).", this);
            }
        }

        // Validate Behaviour arrays
        if (targetBehaviours != null)
        {
            int targetCount = targetBehaviours.Length;
            if (behaviourStateWhenOn != null && behaviourStateWhenOn.Length != targetCount)
            {
                Debug.LogWarning($"[SimpleUdonToggle] '{gameObject.name}': behaviourStateWhenOn array length ({behaviourStateWhenOn.Length}) doesn't match targetBehaviours ({targetCount}).", this);
            }
            if (behaviourStateWhenOff != null && behaviourStateWhenOff.Length != targetCount)
            {
                Debug.LogWarning($"[SimpleUdonToggle] '{gameObject.name}': behaviourStateWhenOff array length ({behaviourStateWhenOff.Length}) doesn't match targetBehaviours ({targetCount}).", this);
            }
        }

        // Validate Udon arrays
        if (udonTargets != null)
        {
            int targetCount = udonTargets.Length;
            if (udonEventWhenOn != null && udonEventWhenOn.Length != targetCount)
            {
                Debug.LogWarning($"[SimpleUdonToggle] '{gameObject.name}': udonEventWhenOn array length ({udonEventWhenOn.Length}) doesn't match udonTargets ({targetCount}).", this);
            }
            if (udonEventWhenOff != null && udonEventWhenOff.Length != targetCount)
            {
                Debug.LogWarning($"[SimpleUdonToggle] '{gameObject.name}': udonEventWhenOff array length ({udonEventWhenOff.Length}) doesn't match udonTargets ({targetCount}).", this);
            }
        }

        // Validate other toggles arrays
        if (otherTogglesToSync != null)
        {
            int targetCount = otherTogglesToSync.Length;
            if (otherTogglesStateWhenOn != null && otherTogglesStateWhenOn.Length != targetCount)
            {
                Debug.LogWarning($"[SimpleUdonToggle] '{gameObject.name}': otherTogglesStateWhenOn array length ({otherTogglesStateWhenOn.Length}) doesn't match otherTogglesToSync ({targetCount}).", this);
            }
            if (otherTogglesStateWhenOff != null && otherTogglesStateWhenOff.Length != targetCount)
            {
                Debug.LogWarning($"[SimpleUdonToggle] '{gameObject.name}': otherTogglesStateWhenOff array length ({otherTogglesStateWhenOff.Length}) doesn't match otherTogglesToSync ({targetCount}).", this);
            }
        }
    }
    #endregion
#endif

    #region Public API
    // Public API: Toggle from other scripts
    public void Toggle()
    {
        SetToggle(!isOn);
    }

    // Set by external scripts; respects network mode rules
    public void SetToggle(bool desired)
    {
        if (networkMode == NetworkMode.Synced)
        {
            // Ensure owner sets the synced variable
            if (!Networking.IsOwner(gameObject) && Networking.LocalPlayer != null)
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }

            if (Networking.IsOwner(gameObject))
            {
                syncedIsOn = desired;
                RequestSerialization();
                isOn = desired;
                ApplyState(isOn);
            }
            else
            {
                // Ownership transfer may not be immediate; still apply locally for feedback
                isOn = desired;
                ApplyState(isOn);
            }
        }
        else if (networkMode == NetworkMode.OwnerOnly)
        {
            if (Networking.IsOwner(gameObject))
            {
                isOn = desired;
                ApplyState(isOn);
            }
            else
            {
                // Notify owner to change state via network event (owner will receive RequestToggleOn/Off)
                string evName = desired ? nameof(RequestToggleOn) : nameof(RequestToggleOff);
                SendCustomNetworkEvent(NetworkEventTarget.Owner, evName);
            }
        }
        else // None
        {
            isOn = desired;
            ApplyState(isOn);
        }

        // Persist locally if requested (skip if not yet restored - OnPlayerRestored will apply saved state)
        if (persistLocally && _restored)
        {
            PlayerData.SetBool(_computedPersistenceKey, isOn);
        }
    }
    #endregion

    #region Network Events
    // Owner-only network events receivers
    public void RequestToggleOn()
    {
        if (Networking.IsOwner(gameObject))
        {
            SetToggle(true);
        }
    }
    public void RequestToggleOff()
    {
        if (Networking.IsOwner(gameObject))
        {
            SetToggle(false);
        }
    }

    // Called when synced data arrives
    public override void OnDeserialization()
    {
        // Mark that we've received sync data
        _syncReceived = true;
        
        // Update local copy from synced
        isOn = syncedIsOn;
        ApplyState(isOn);
    
        #if UNITY_EDITOR
        Debug.Log($"[SimpleUdonToggle] OnDeserialization. syncedIsOn: {syncedIsOn}");
        #endif
    }
    
    public override void OnPlayerRestored(VRCPlayerApi player)
    {
        if (!player.isLocal || !persistLocally) return;
    
        _restored = true;
    
        bool restoredOnState;
        if (PlayerData.TryGetBool(Networking.LocalPlayer, _computedPersistenceKey, out restoredOnState))
        {
            isOn = restoredOnState;
        }
        // If key doesn't exist, 'isOn' remains what was set in Start (defaultOn).
    
        ApplyState(isOn);
    
        #if UNITY_EDITOR
        Debug.Log($"[SimpleUdonToggle] Player data restored. Key: '{_computedPersistenceKey}'. Applied state: {isOn}");
        #endif
    }
    #endregion

    #region State Application
    private void ApplyState(bool on)
    {
        // Update UI toggles without invoking their callbacks
        if (updateUITogglesOnStart && uiToggles != null)
        {
            foreach (Toggle t in uiToggles)
            {
                if (t != null)
                {
                    t.SetIsOnWithoutNotify(on);
                }
            }
        }

        // Apply to GameObjects arrays
        if (targetGameObjects != null)
        {
            for (int i = 0; i < targetGameObjects.Length; i++)
            {
                if (targetGameObjects[i] == null) continue;
                bool desired = (on ? (gameObjectStateWhenOn != null && gameObjectStateWhenOn.Length > i ? gameObjectStateWhenOn[i] : true)
                                   : (gameObjectStateWhenOff != null && gameObjectStateWhenOff.Length > i ? gameObjectStateWhenOff[i] : false));
                targetGameObjects[i].SetActive(desired);
            }
        }

        // Apply to Behaviours enable/disable
        if (targetBehaviours != null)
        {
            for (int i = 0; i < targetBehaviours.Length; i++)
            {
                if (targetBehaviours[i] == null) continue;
                bool desired = (on ? (behaviourStateWhenOn != null && behaviourStateWhenOn.Length > i ? behaviourStateWhenOn[i] : true)
                                   : (behaviourStateWhenOff != null && behaviourStateWhenOff.Length > i ? behaviourStateWhenOff[i] : false));
                targetBehaviours[i].enabled = desired;
            }
        }

        // Send events to UdonTargets for this transition
        if (udonTargets != null)
        {
            for (int i = 0; i < udonTargets.Length; i++)
            {
                if (udonTargets[i] == null) continue;
                string ev = on ? (udonEventWhenOn != null && udonEventWhenOn.Length > i ? udonEventWhenOn[i] : null)
                               : (udonEventWhenOff != null && udonEventWhenOff.Length > i ? udonEventWhenOff[i] : null);
                if (!string.IsNullOrEmpty(ev))
                {
                    udonTargets[i].SendCustomEvent(ev);
                }
            }
        }

        // Control other toggles
        if (otherTogglesToSync != null)
        {
            for (int i = 0; i < otherTogglesToSync.Length; i++)
            {
                if (otherTogglesToSync[i] == null) continue;
                bool desired = (on ? (otherTogglesStateWhenOn != null && otherTogglesStateWhenOn.Length > i ? otherTogglesStateWhenOn[i] : on)
                                   : (otherTogglesStateWhenOff != null && otherTogglesStateWhenOff.Length > i ? otherTogglesStateWhenOff[i] : !on));
                otherTogglesToSync[i].SetToggle(desired);
            }
        }

        #if UNITY_EDITOR
        Debug.Log($"[SimpleUdonToggle] ApplyState -> {(on ? "ON" : "OFF")}");
        #endif
    }
    #endregion

    #region VRC Callbacks
    // Interact support for click-based toggling
    public override void Interact()
    {
        if (!allowInteract) return;

        // For OwnerOnly, route via SetToggle which will notify owner if needed
        Toggle();
    }

    // Optional trigger-based toggling (local player only)
    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (!toggleOnTriggerEnter) return;
        if (!Utilities.IsValid(player) || !player.isLocal) return;

        Toggle();
    }

    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        if (!toggleOnTriggerExit) return;
        if (!Utilities.IsValid(player) || !player.isLocal) return;

        Toggle();
    }
    #endregion

    #region UI Callbacks
    // UI callback for Toggle components (passes bool parameter)
    // OnUiToggleChanged is public so it can be called from a Unity Event in the inspector.
    public void OnUiToggleChanged(bool val)
    {
        if (_suppressUiCallback) return;
        SetToggle(val);
    }

    // UI callback for Button components (no parameters - toggles current state)
    public void OnUiButtonClicked()
    {
        Toggle();
#if UNITY_EDITOR
        Debug.Log($"[SimpleUdonToggle] OnUiButtonClicked. New state: {isOn}" );
#endif
    }
    #endregion
}
