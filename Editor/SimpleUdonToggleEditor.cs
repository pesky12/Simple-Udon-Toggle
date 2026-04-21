using System;
using System.Reflection;
using UnityEditor;
#if UNITY_EDITOR
using UnityEditor.Events;
#endif
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UdonSharp;
using VRC.Udon;

[CustomEditor(typeof(SimpleUdonToggle))]
public class SimpleUdonToggleEditor : Editor
{
    // --- Visual Styles ---
    private GUIStyle _onSectionStyle;
    private GUIStyle _offSectionStyle;
    private GUIContent _onIcon;
    private GUIContent _offIcon;
    private GUIContent _autoAssignIcon;
    private GUIContent _settingsIcon;
    
    // Colors for sections
    private static readonly Color OnColor = new Color(0.2f, 0.6f, 0.2f, 0.3f);  // Green tint
    private static readonly Color OffColor = new Color(0.6f, 0.2f, 0.2f, 0.3f); // Red tint

    // --- Main Properties ---
    SerializedProperty toggleNameProp;
    SerializedProperty defaultOnProp;
    SerializedProperty networkModeProp;
    SerializedProperty persistLocallyProp;
    SerializedProperty persistenceKeyProp;
    SerializedProperty allowInteractProp;
    SerializedProperty toggleOnTriggerEnterProp;
    SerializedProperty toggleOnTriggerExitProp;
    SerializedProperty uiTogglesProp;
    SerializedProperty updateUITogglesOnStartProp;

    // --- GameObject Arrays ---
    SerializedProperty targetGameObjectsProp;
    SerializedProperty gameObjectStateWhenOnProp;
    SerializedProperty gameObjectStateWhenOffProp;

    // --- Behaviour Arrays ---
    SerializedProperty targetBehavioursProp;
    SerializedProperty behaviourStateWhenOnProp;
    SerializedProperty behaviourStateWhenOffProp;

    // --- Udon Target Arrays ---
    SerializedProperty udonTargetsProp;
    SerializedProperty udonEventWhenOnProp;
    SerializedProperty udonEventWhenOffProp;
    
    // --- Other Toggles ---
    SerializedProperty otherTogglesToSyncProp;
    SerializedProperty otherTogglesStateWhenOnProp;
    SerializedProperty otherTogglesStateWhenOffProp;
    
    private void OnEnable()
    {
        // --- Initialize Visual Styles ---
        InitializeStyles();
        
        // --- Main Properties ---
        toggleNameProp = serializedObject.FindProperty("toggleName");
        defaultOnProp = serializedObject.FindProperty("defaultOn");
        networkModeProp = serializedObject.FindProperty("networkMode");
        persistLocallyProp = serializedObject.FindProperty("persistLocally");
        persistenceKeyProp = serializedObject.FindProperty("persistenceKey");
        allowInteractProp = serializedObject.FindProperty("allowInteract");
        toggleOnTriggerEnterProp = serializedObject.FindProperty("toggleOnTriggerEnter");
        toggleOnTriggerExitProp = serializedObject.FindProperty("toggleOnTriggerExit");
        uiTogglesProp = serializedObject.FindProperty("uiToggles");
        updateUITogglesOnStartProp = serializedObject.FindProperty("updateUITogglesOnStart");

        // --- GameObject Arrays ---
        targetGameObjectsProp = serializedObject.FindProperty("targetGameObjects");
        gameObjectStateWhenOnProp = serializedObject.FindProperty("gameObjectStateWhenOn");
        gameObjectStateWhenOffProp = serializedObject.FindProperty("gameObjectStateWhenOff");

        // --- Behaviour Arrays ---
        targetBehavioursProp = serializedObject.FindProperty("targetBehaviours");
        behaviourStateWhenOnProp = serializedObject.FindProperty("behaviourStateWhenOn");
        behaviourStateWhenOffProp = serializedObject.FindProperty("behaviourStateWhenOff");

        // --- Udon Target Arrays ---
        udonTargetsProp = serializedObject.FindProperty("udonTargets");
        udonEventWhenOnProp = serializedObject.FindProperty("udonEventWhenOn");
        udonEventWhenOffProp = serializedObject.FindProperty("udonEventWhenOff");
    
        // --- Other Toggles ---
        otherTogglesToSyncProp = serializedObject.FindProperty("otherTogglesToSync");
        otherTogglesStateWhenOnProp = serializedObject.FindProperty("otherTogglesStateWhenOn");
        otherTogglesStateWhenOffProp = serializedObject.FindProperty("otherTogglesStateWhenOff");
    }
    
    private void InitializeStyles()
    {
        // ON section style (green tint)
        _onSectionStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(10, 10, 8, 8)
        };
        
        // OFF section style (red tint)
        _offSectionStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(10, 10, 8, 8)
        };
        
        // Icons (Unity built-in)
        _onIcon = EditorGUIUtility.IconContent("d_PlayButton");
        _offIcon = EditorGUIUtility.IconContent("d_PauseButton");
        _autoAssignIcon = EditorGUIUtility.IconContent("d_SceneViewTools");
        _settingsIcon = EditorGUIUtility.IconContent("d_Settings");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // --- Auto-Assignment Section ---
        DrawSectionHeader(_autoAssignIcon, "Auto-Assignment");
        
        EditorGUILayout.PropertyField(toggleNameProp, new GUIContent("Toggle Name"));
        
        SimpleUdonToggle script = (SimpleUdonToggle)target;
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Auto-Assign All Targets by Name", GUILayout.Height(24)))
        {
            SetupForInstance(script);
            EditorUtility.SetDirty(target);
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Setup All SimpleUdonToggles in Scene", GUILayout.Height(24)))
        {
            SetupAllInScene();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.HelpBox("Place SimpleToggleMarker components on GameObjects with matching 'Toggle Name' to auto-assign them.", MessageType.Info);

        EditorGUILayout.Space(4);
        DrawSectionHeader(_settingsIcon, "Core Settings");
        EditorGUILayout.PropertyField(defaultOnProp);
        EditorGUILayout.PropertyField(networkModeProp);
        
        // Persistence is only valid for non-synced modes
        NetworkMode currentMode = (NetworkMode)networkModeProp.enumValueIndex;
        if (currentMode == NetworkMode.Synced)
        {
            // Disable persistence option for synced mode
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(persistLocallyProp);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.HelpBox("Persistence is not available for Synced mode. Synced toggles use network state", MessageType.Info);
        }
        else
        {
            EditorGUILayout.PropertyField(persistLocallyProp);
            if (persistLocallyProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(persistenceKeyProp, new GUIContent("Persistence Key"));
                if (string.IsNullOrEmpty(persistenceKeyProp.stringValue))
                {
                    EditorGUILayout.HelpBox("A unique Persistence Key is required to save the toggle state.", MessageType.Warning);
                }
                EditorGUI.indentLevel--;
            }
        }
        
        EditorGUILayout.Space(4);
        DrawSectionHeader(EditorGUIUtility.IconContent("d_PreMatCube"), "Activation Methods");
        EditorGUILayout.PropertyField(allowInteractProp, new GUIContent("Allow Click Toggle (Interact)"));
        EditorGUILayout.PropertyField(toggleOnTriggerEnterProp);
        EditorGUILayout.PropertyField(toggleOnTriggerExitProp);

        EditorGUILayout.Space(4);
        DrawSectionHeader(EditorGUIUtility.IconContent("d_Toggle Icon"), "UI Linking");
        EditorGUILayout.PropertyField(uiTogglesProp, true);
        if (uiTogglesProp.arraySize > 0)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(updateUITogglesOnStartProp);
            EditorGUILayout.HelpBox("To allow the UI Toggles to control this script, you must manually wire each of their 'On Value Changed' events in the Inspector to this script's 'OnUiButtonClicked' public function.", MessageType.Info);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(4);
        DrawSectionHeader(EditorGUIUtility.IconContent("d_PreMatQuad"), "Actions");
        
        // --- ON STATE (Green tinted) ---
        Color prevBgColor = GUI.backgroundColor;
        GUI.backgroundColor = OnColor;
        EditorGUILayout.BeginVertical(_onSectionStyle);
        GUI.backgroundColor = prevBgColor;
        EditorGUI.indentLevel++;
        DrawStateHeader(_onIcon, "When Toggle is ON", new Color(0.3f, 0.7f, 0.3f));
        DrawSimpleList(targetGameObjectsProp, gameObjectStateWhenOnProp, "GameObjects", "d_GameObject Icon", "Active");
        DrawSimpleList(targetBehavioursProp, behaviourStateWhenOnProp, "Components", "d_ScriptableObject Icon", "Enabled");
        DrawUdonSimpleList(udonTargetsProp, udonEventWhenOnProp, "Udon Events", "d_cs Script Icon");
        DrawOtherTogglesList(otherTogglesToSyncProp, otherTogglesStateWhenOnProp, "Other Toggles", "d_Toggle Icon", "Set ON");
        EditorGUI.indentLevel--;
        EditorGUILayout.EndVertical();

        // --- OFF STATE (Red tinted) ---
        EditorGUILayout.Space(4);
        GUI.backgroundColor = OffColor;
        EditorGUILayout.BeginVertical(_offSectionStyle);
        GUI.backgroundColor = prevBgColor;
        EditorGUI.indentLevel++;
        DrawStateHeader(_offIcon, "When Toggle is OFF", new Color(0.7f, 0.3f, 0.3f));
        DrawSimpleList(targetGameObjectsProp, gameObjectStateWhenOffProp, "GameObjects", "d_GameObject Icon", "Active");
        DrawSimpleList(targetBehavioursProp, behaviourStateWhenOffProp, "Components", "d_ScriptableObject Icon", "Enabled");
        DrawUdonSimpleList(udonTargetsProp, udonEventWhenOffProp, "Udon Events", "d_cs Script Icon");
        DrawOtherTogglesList(otherTogglesToSyncProp, otherTogglesStateWhenOffProp, "Other Toggles", "d_Toggle Icon", "Set ON");
        EditorGUI.indentLevel--;
        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
    }
    
    private void DrawSectionHeader(GUIContent icon, string title)
    {
        EditorGUILayout.BeginHorizontal();
        
        // Icon inline with text
        if (icon != null)
        {
            GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(18));
        }
        
        // Bold title text
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawStateHeader(GUIContent icon, string title, Color accentColor)
    {
        EditorGUILayout.BeginHorizontal();
        GUIContent content = icon != null ? new GUIContent(title, icon.image) : new GUIContent(title);
        GUIStyle coloredLabel = new GUIStyle(EditorStyles.boldLabel)
        {
            normal = { textColor = accentColor }
        };
        EditorGUILayout.LabelField(content, coloredLabel, GUILayout.Height(20));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSimpleList(SerializedProperty listProp, SerializedProperty stateProp, string header, string iconKey = null, string stateLabel = "On")
    {
        GUIContent headerContent = iconKey != null && EditorGUIUtility.IconContent(iconKey) != null
            ? new GUIContent(header, EditorGUIUtility.IconContent(iconKey).image)
            : new GUIContent(header);
        EditorGUILayout.LabelField(headerContent, EditorStyles.miniBoldLabel);
        
        // Ensure arrays are sized correctly
        if (stateProp.arraySize != listProp.arraySize) stateProp.arraySize = listProp.arraySize;

        for (int i = 0; i < listProp.arraySize; i++)
        {
            Rect rowRect = EditorGUILayout.GetControlRect();

            if (DrawObjectStateRow(
                rowRect,
                listProp.GetArrayElementAtIndex(i),
                stateProp.GetArrayElementAtIndex(i),
                stateLabel))
            {
                // Delete from all three arrays at the same index to keep them in sync
                // Break after deletion since array size has changed
                listProp.DeleteArrayElementAtIndex(i);
                GetOtherStateProp(stateProp).DeleteArrayElementAtIndex(i);
                stateProp.DeleteArrayElementAtIndex(i);
                break;
            }
        }

        if (GUILayout.Button($"Add New {header.Replace("s","")} Target"))
        {
            listProp.arraySize++;
        }
        EditorGUILayout.Space();
    }

    private bool DrawObjectStateRow(Rect rowRect, SerializedProperty objectProp, SerializedProperty stateProp, string stateLabel)
    {
        const float spacing = 4f;
        const float buttonWidth = 20f;

        int previousIndentLevel = EditorGUI.indentLevel;
        Rect contentRect = EditorGUI.IndentedRect(rowRect);
        EditorGUI.indentLevel = 0;

        GUIContent stateContent = new GUIContent(stateLabel);
        float stateWidth = Mathf.Max(68f, EditorStyles.toggle.CalcSize(stateContent).x + 20f);
        float objectWidth = Mathf.Max(0f, contentRect.width - stateWidth - buttonWidth - (spacing * 2f));

        Rect objectRect = new Rect(contentRect.x, contentRect.y, objectWidth, contentRect.height);
        Rect stateRect = new Rect(objectRect.xMax + spacing, contentRect.y, stateWidth, contentRect.height);
        Rect buttonRect = new Rect(stateRect.xMax + spacing, contentRect.y, buttonWidth, contentRect.height);

        EditorGUI.PropertyField(objectRect, objectProp, GUIContent.none);
        stateProp.boolValue = EditorGUI.ToggleLeft(stateRect, stateContent, stateProp.boolValue);

        bool remove = GUI.Button(buttonRect, "-");
        EditorGUI.indentLevel = previousIndentLevel;
        return remove;
    }

    private void DrawUdonSimpleList(SerializedProperty listProp, SerializedProperty eventProp, string header, string iconKey = null)
    {
        GUIContent headerContent = iconKey != null && EditorGUIUtility.IconContent(iconKey) != null
            ? new GUIContent(header, EditorGUIUtility.IconContent(iconKey).image)
            : new GUIContent(header);
        EditorGUILayout.LabelField(headerContent, EditorStyles.miniBoldLabel);

        // Ensure arrays are sized correctly
        if (eventProp.arraySize != listProp.arraySize) eventProp.arraySize = listProp.arraySize;

        for (int i = 0; i < listProp.arraySize; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(listProp.GetArrayElementAtIndex(i), GUIContent.none);
            EditorGUILayout.PropertyField(eventProp.GetArrayElementAtIndex(i), GUIContent.none);

            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                // Delete from all three arrays at the same index to keep them in sync
                // Break after deletion since array size has changed
                listProp.DeleteArrayElementAtIndex(i);
                GetOtherStateProp(eventProp).DeleteArrayElementAtIndex(i);
                eventProp.DeleteArrayElementAtIndex(i);
                break;
            }
            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button($"Add New {header.Replace("s","")} Target"))
        {
            listProp.arraySize++;
        }
        EditorGUILayout.Space();
    }

    private void DrawOtherTogglesList(SerializedProperty listProp, SerializedProperty stateProp, string header, string iconKey = null, string stateLabel = "ON")
    {
        GUIContent headerContent = iconKey != null && EditorGUIUtility.IconContent(iconKey) != null
            ? new GUIContent(header, EditorGUIUtility.IconContent(iconKey).image)
            : new GUIContent(header);
        EditorGUILayout.LabelField(headerContent, EditorStyles.miniBoldLabel);

        // Ensure arrays are sized correctly
        if (stateProp.arraySize != listProp.arraySize) stateProp.arraySize = listProp.arraySize;

        for (int i = 0; i < listProp.arraySize; i++)
        {
            Rect rowRect = EditorGUILayout.GetControlRect();

            if (DrawObjectStateRow(
                rowRect,
                listProp.GetArrayElementAtIndex(i),
                stateProp.GetArrayElementAtIndex(i),
                stateLabel))
            {
                // Delete from all three arrays at the same index to keep them in sync
                // Break after deletion since array size has changed
                listProp.DeleteArrayElementAtIndex(i);
                GetOtherStateProp(stateProp).DeleteArrayElementAtIndex(i);
                stateProp.DeleteArrayElementAtIndex(i);
                break;
            }
        }

        if (GUILayout.Button($"Add New {header.Replace("s","")} Target"))
        {
            listProp.arraySize++;
        }
        EditorGUILayout.Space();
    }

    private SerializedProperty GetOtherStateProp(SerializedProperty currentProp)
    {
        if (currentProp.name == gameObjectStateWhenOnProp.name) return gameObjectStateWhenOffProp;
        if (currentProp.name == gameObjectStateWhenOffProp.name) return gameObjectStateWhenOnProp;
        if (currentProp.name == behaviourStateWhenOnProp.name) return behaviourStateWhenOffProp;
        if (currentProp.name == behaviourStateWhenOffProp.name) return behaviourStateWhenOnProp;
        if (currentProp.name == udonEventWhenOnProp.name) return udonEventWhenOffProp;
        if (currentProp.name == udonEventWhenOffProp.name) return udonEventWhenOnProp;
        if (currentProp.name == otherTogglesStateWhenOnProp.name) return otherTogglesStateWhenOffProp;
        if (currentProp.name == otherTogglesStateWhenOffProp.name) return otherTogglesStateWhenOnProp;
        return null;
    }

    // --- Auto-Assignment Helper Methods ---
    private void SetupAllInScene()
    {
        // Setup all instances in open scenes
        var sceneInstances = FindObjectsOfType<SimpleUdonToggle>(true);
        foreach (var inst in sceneInstances)
        {
            SetupForInstance(inst);
            EditorUtility.SetDirty(inst);
        }

        // Setup all prefab assets that contain the component
        var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        foreach (var guid in prefabGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) continue;
            var comp = go.GetComponent<SimpleUdonToggle>();
            if (comp == null) continue;
            SetupForInstance(comp);
            EditorUtility.SetDirty(go);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[SimpleUdonToggle] Setup complete for all instances!");
    }

    private static void SetupForInstance(SimpleUdonToggle script)
    {
        if (string.IsNullOrEmpty(script.toggleName))
        {
            Debug.LogWarning($"[SimpleUdonToggle] Cannot auto-assign: toggleName is empty on {script.gameObject.name}", script);
            return;
        }

        // Record undo for this operation
        Undo.RecordObject(script, "Auto-Assign Toggle Targets");

        // Find all markers in the scene
        SimpleToggleMarker[] markers = FindObjectsOfType<SimpleToggleMarker>(true);
        UdonBehaviour thisBehaviour = script.GetComponent<UdonBehaviour>();
        string toggleName = script.toggleName;

        // Use SerializedObject to read private fields
        SerializedObject so = new SerializedObject(script);
        so.Update();

        // Initialize lists with EXISTING assignments to preserve user changes
        var uiTogglesList = new System.Collections.Generic.List<Toggle>();
        if (script.uiToggles != null) uiTogglesList.AddRange(script.uiToggles);
        
        var targetGameObjectsList = GetExistingArray<GameObject>(so, "targetGameObjects");
        var gameObjectStateWhenOnList = GetExistingBoolArray(so, "gameObjectStateWhenOn");
        var gameObjectStateWhenOffList = GetExistingBoolArray(so, "gameObjectStateWhenOff");
        // Ensure state lists match target count
        while (gameObjectStateWhenOnList.Count < targetGameObjectsList.Count) gameObjectStateWhenOnList.Add(true);
        while (gameObjectStateWhenOffList.Count < targetGameObjectsList.Count) gameObjectStateWhenOffList.Add(false);

        var targetBehavioursList = GetExistingArray<Behaviour>(so, "targetBehaviours");
        var behaviourStateWhenOnList = GetExistingBoolArray(so, "behaviourStateWhenOn");
        var behaviourStateWhenOffList = GetExistingBoolArray(so, "behaviourStateWhenOff");
        while (behaviourStateWhenOnList.Count < targetBehavioursList.Count) behaviourStateWhenOnList.Add(true);
        while (behaviourStateWhenOffList.Count < targetBehavioursList.Count) behaviourStateWhenOffList.Add(false);

        var udonTargetsList = GetExistingArray<UdonBehaviour>(so, "udonTargets");
        var udonEventWhenOnList = GetExistingStringArray(so, "udonEventWhenOn");
        var udonEventWhenOffList = GetExistingStringArray(so, "udonEventWhenOff");
        while (udonEventWhenOnList.Count < udonTargetsList.Count) udonEventWhenOnList.Add("");
        while (udonEventWhenOffList.Count < udonTargetsList.Count) udonEventWhenOffList.Add("");

        var otherTogglesToSyncList = GetExistingArray<SimpleUdonToggle>(so, "otherTogglesToSync");
        var otherTogglesStateWhenOnList = GetExistingBoolArray(so, "otherTogglesStateWhenOn");
        var otherTogglesStateWhenOffList = GetExistingBoolArray(so, "otherTogglesStateWhenOff");
        while (otherTogglesStateWhenOnList.Count < otherTogglesToSyncList.Count) otherTogglesStateWhenOnList.Add(true);
        while (otherTogglesStateWhenOffList.Count < otherTogglesToSyncList.Count) otherTogglesStateWhenOffList.Add(false);

        int foundCount = 0;
        foreach (var marker in markers)
        {
            if (marker.toggleName != toggleName) continue;

            foundCount++;
            
            switch (marker.targetType)
            {
                case ToggleTargetType.UiToggle:
                    var markerToggle = marker.targetUiToggle;
                    if (markerToggle == null) markerToggle = marker.GetComponent<Toggle>();

                    if (markerToggle != null)
                    {
                        // Only add if not already in the list
                        if (!uiTogglesList.Contains(markerToggle))
                        {
                            uiTogglesList.Add(markerToggle);
                        }
                        SetupToggleButton(thisBehaviour, markerToggle, nameof(SimpleUdonToggle.OnUiButtonClicked));
                    }
                    else
                    {
                        Debug.LogWarning($"[SimpleUdonToggle] Marker on {marker.name} expects UI Toggle but none found/assigned.", marker);
                    }
                    break;

                case ToggleTargetType.GameObject:
                    var targetGO = marker.targetGameObject;
                    if (targetGO == null) targetGO = marker.gameObject;
                    
                    // Only add if not already in the list
                    if (!targetGameObjectsList.Contains(targetGO))
                    {
                        targetGameObjectsList.Add(targetGO);
                        gameObjectStateWhenOnList.Add(marker.stateWhenOn);
                        gameObjectStateWhenOffList.Add(marker.stateWhenOff);
                    }
                    break;

                case ToggleTargetType.Component:
                    var targetComp = marker.targetBehaviour;
                    if (targetComp == null)
                    {
                        // Fallback: try common Behaviours
                        targetComp = marker.GetComponent<Light>();
                        if (targetComp == null) targetComp = marker.GetComponent<Camera>();
                        if (targetComp == null) targetComp = marker.GetComponent<AudioSource>();
                        if (targetComp == null) targetComp = marker.GetComponent<Animator>();
                    }

                    if (targetComp is Behaviour behaviour)
                    {
                        // Only add if not already in the list
                        if (!targetBehavioursList.Contains(behaviour))
                        {
                            targetBehavioursList.Add(behaviour);
                            behaviourStateWhenOnList.Add(marker.stateWhenOn);
                            behaviourStateWhenOffList.Add(marker.stateWhenOff);
                        }
                    }
                    else if (targetComp != null)
                    {
                        Debug.LogWarning($"[SimpleUdonToggle] Marker on {marker.name} has '{targetComp.GetType().Name}' which is not a Behaviour. Use GameObject target type to toggle active state instead.", marker);
                    }
                    else
                    {
                        Debug.LogWarning($"[SimpleUdonToggle] Marker on {marker.name} set to 'Component' but no Behaviour found/assigned.", marker);
                    }
                    break;

                case ToggleTargetType.UdonEvent:
                    var udon = marker.targetUdon;
                    if (udon == null) udon = marker.GetComponent<UdonBehaviour>();

                    if (udon != null)
                    {
                        // Only add if not already in the list
                        if (!udonTargetsList.Contains(udon))
                        {
                            udonTargetsList.Add(udon);
                            udonEventWhenOnList.Add(marker.eventNameOn);
                            udonEventWhenOffList.Add(marker.eventNameOff);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[SimpleUdonToggle] Marker on {marker.name} expects UdonBehaviour but none found/assigned.", marker);
                    }
                    break;

                case ToggleTargetType.OtherToggle:
                    var otherToggle = marker.targetSimpleToggle;
                    if (otherToggle == null) otherToggle = marker.GetComponent<SimpleUdonToggle>();

                    if (otherToggle != null)
                    {
                        // Only add if not already in the list
                        if (!otherTogglesToSyncList.Contains(otherToggle))
                        {
                            otherTogglesToSyncList.Add(otherToggle);
                            otherTogglesStateWhenOnList.Add(marker.stateWhenOn);
                            otherTogglesStateWhenOffList.Add(marker.stateWhenOff);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[SimpleUdonToggle] Marker on {marker.name} expects SimpleUdonToggle but none found/assigned.", marker);
                    }
                    break;
            }
        }

        // Apply back to arrays (merges with existing assignments)
        SetArray(so, "uiToggles", uiTogglesList.ToArray());

        // Other target arrays
        SetArray(so, "targetGameObjects", targetGameObjectsList.ToArray());
        SetArray(so, "gameObjectStateWhenOn", gameObjectStateWhenOnList.ToArray());
        SetArray(so, "gameObjectStateWhenOff", gameObjectStateWhenOffList.ToArray());

        SetArray(so, "targetBehaviours", targetBehavioursList.ToArray());
        SetArray(so, "behaviourStateWhenOn", behaviourStateWhenOnList.ToArray());
        SetArray(so, "behaviourStateWhenOff", behaviourStateWhenOffList.ToArray());

        SetArray(so, "udonTargets", udonTargetsList.ToArray());
        SetArray(so, "udonEventWhenOn", udonEventWhenOnList.ToArray());
        SetArray(so, "udonEventWhenOff", udonEventWhenOffList.ToArray());

        SetArray(so, "otherTogglesToSync", otherTogglesToSyncList.ToArray());
        SetArray(so, "otherTogglesStateWhenOn", otherTogglesStateWhenOnList.ToArray());
        SetArray(so, "otherTogglesStateWhenOff", otherTogglesStateWhenOffList.ToArray());

        so.ApplyModifiedProperties();
        
        Debug.Log($"[SimpleUdonToggle] Setup '{toggleName}' complete. Found {foundCount} markers.");
    }

    private static void SetupToggleButton(UdonBehaviour toCall, Toggle toggle, string methodName)
    {
        // Check if our listener already exists to avoid duplicates and preserve user-added listeners
        var listenerCount = toggle.onValueChanged.GetPersistentEventCount();
        for (int i = 0; i < listenerCount; i++)
        {
            var target = toggle.onValueChanged.GetPersistentTarget(i);
            var method = toggle.onValueChanged.GetPersistentMethodName(i);
            
            // If this exact listener already exists, skip adding it
            if (target == toCall && method == "SendCustomEvent")
            {
                return;
            }
        }

        // Record undo before modifying the toggle
        Undo.RecordObject(toggle, "Setup Toggle Listener");

        // Add the listener for OnUiButtonClicked
        // Note: Uses reflection to access SendCustomEvent on UdonBehaviour
        // If VRChat SDK changes internal API, this will log a clear error
        try
        {
            var methodInfo = toCall.GetType().GetMethod("SendCustomEvent",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            
            if (methodInfo == null)
            {
                Debug.LogError($"[SimpleUdonToggle] Could not find SendCustomEvent method on UdonBehaviour. The VRChat SDK may have changed. Please report this issue.", toCall);
                return;
            }
            
            var action = Delegate.CreateDelegate(typeof(UnityAction<string>), toCall, methodInfo) as UnityAction<string>;
            UnityEventTools.AddStringPersistentListener(toggle.onValueChanged, action, methodName);
            EditorUtility.SetDirty(toggle);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SimpleUdonToggle] Failed to setup toggle listener: {ex.Message}. The VRChat SDK may have changed.", toCall);
        }
    }
    
    // --- Helper Methods ---

    private static void SetArray<T>(SerializedObject so, string propName, T[] array) where T : UnityEngine.Object
    {
        SerializedProperty prop = so.FindProperty(propName);
        if (prop == null)
        {
            Debug.LogError($"[SimpleUdonToggle] Property '{propName}' not found!");
            return;
        }
        
        prop.ClearArray();
        prop.arraySize = array.Length;
        for (int i = 0; i < array.Length; i++)
        {
            prop.GetArrayElementAtIndex(i).objectReferenceValue = array[i];
        }
    }
    
    private static void SetArray(SerializedObject so, string propName, bool[] array)
    {
        SerializedProperty prop = so.FindProperty(propName);
        if (prop == null)
        {
            Debug.LogError($"[SimpleUdonToggle] Property '{propName}' not found!");
            return;
        }
        
        prop.ClearArray();
        prop.arraySize = array.Length;
        for (int i = 0; i < array.Length; i++)
        {
            prop.GetArrayElementAtIndex(i).boolValue = array[i];
        }
    }

    private static void SetArray(SerializedObject so, string propName, string[] array)
    {
        SerializedProperty prop = so.FindProperty(propName);
        if (prop == null)
        {
            Debug.LogError($"[SimpleUdonToggle] Property '{propName}' not found!");
            return;
        }
        
        prop.ClearArray();
        prop.arraySize = array.Length;
        for (int i = 0; i < array.Length; i++)
        {
            prop.GetArrayElementAtIndex(i).stringValue = array[i];
        }
    }

    // Helper methods to read existing arrays from SerializedObject
    private static System.Collections.Generic.List<T> GetExistingArray<T>(SerializedObject so, string propName) where T : UnityEngine.Object
    {
        var list = new System.Collections.Generic.List<T>();
        SerializedProperty prop = so.FindProperty(propName);
        if (prop != null && prop.isArray)
        {
            for (int i = 0; i < prop.arraySize; i++)
            {
                var element = prop.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue is T t)
                {
                    list.Add(t);
                }
            }
        }
        return list;
    }

    private static System.Collections.Generic.List<bool> GetExistingBoolArray(SerializedObject so, string propName)
    {
        var list = new System.Collections.Generic.List<bool>();
        SerializedProperty prop = so.FindProperty(propName);
        if (prop != null && prop.isArray)
        {
            for (int i = 0; i < prop.arraySize; i++)
            {
                list.Add(prop.GetArrayElementAtIndex(i).boolValue);
            }
        }
        return list;
    }

    private static System.Collections.Generic.List<string> GetExistingStringArray(SerializedObject so, string propName)
    {
        var list = new System.Collections.Generic.List<string>();
        SerializedProperty prop = so.FindProperty(propName);
        if (prop != null && prop.isArray)
        {
            for (int i = 0; i < prop.arraySize; i++)
            {
                list.Add(prop.GetArrayElementAtIndex(i).stringValue);
            }
        }
        return list;
    }
}