using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VRC.Udon;

[CustomEditor(typeof(SimpleToggleMarker))]
public class SimpleToggleMarkerEditor : Editor
{
    private GUIStyle _onSectionStyle;
    private GUIStyle _offSectionStyle;
    private GUIContent _onIcon;
    private GUIContent _offIcon;
    private GUIContent _settingsIcon;

    private SerializedProperty toggleNameProp;
    private SerializedProperty targetTypeProp;
    
    private SerializedProperty stateWhenOnProp;
    private SerializedProperty stateWhenOffProp;
    
    private SerializedProperty targetComponentProp;
    private SerializedProperty targetUdonProp;
    private SerializedProperty targetUiToggleProp;
    private SerializedProperty targetSimpleToggleProp;
    private SerializedProperty targetGameObjectProp;
    
    private SerializedProperty eventNameOnProp;
    private SerializedProperty eventNameOffProp;

    private void OnEnable()
    {
        InitializeStyles();

        toggleNameProp = serializedObject.FindProperty("toggleName");
        targetTypeProp = serializedObject.FindProperty("targetType");
        
        stateWhenOnProp = serializedObject.FindProperty("stateWhenOn");
        stateWhenOffProp = serializedObject.FindProperty("stateWhenOff");

        targetComponentProp = serializedObject.FindProperty("targetComponent");
        targetUdonProp = serializedObject.FindProperty("targetUdon");
        targetUiToggleProp = serializedObject.FindProperty("targetUiToggle");
        targetSimpleToggleProp = serializedObject.FindProperty("targetSimpleToggle");
        targetGameObjectProp = serializedObject.FindProperty("targetGameObject");

        eventNameOnProp = serializedObject.FindProperty("eventNameOn");
        eventNameOffProp = serializedObject.FindProperty("eventNameOff");
    }

    private void InitializeStyles()
    {
        _onSectionStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(10, 10, 8, 8)
        };

        _offSectionStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(10, 10, 8, 8)
        };

        _onIcon = EditorGUIUtility.IconContent("d_PlayButton");
        _offIcon = EditorGUIUtility.IconContent("d_PauseButton");
        _settingsIcon = EditorGUIUtility.IconContent("d_SceneViewTools");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        SimpleToggleMarker marker = (SimpleToggleMarker)target;

        DrawSectionHeader(_settingsIcon, "Marker Settings");
        EditorGUILayout.PropertyField(toggleNameProp, new GUIContent("Toggle Name"));
        EditorGUILayout.PropertyField(targetTypeProp, new GUIContent("Marker Mode"));
        EditorGUILayout.Space(4);

        ToggleTargetType type = (ToggleTargetType)targetTypeProp.enumValueIndex;

        switch (type)
        {
            case ToggleTargetType.UiToggle:
                DrawSectionHeader(EditorGUIUtility.IconContent("d_Toggle Icon"), "UI Link");
                DrawTargetField(targetUiToggleProp, marker.gameObject, typeof(Toggle), "Target UI Toggle");
                EditorGUILayout.HelpBox("This marker will treat the UI Toggle as the controller/display for the SimpleUdonToggle.", MessageType.Info);
                break;

            case ToggleTargetType.GameObject:
                DrawSectionHeader(EditorGUIUtility.IconContent("d_GameObject Icon"), "Target");
                // For GameObject, we usually target the one the marker is on, but allow override
                EditorGUILayout.HelpBox("Target GameObject defaults to THIS object if left empty.", MessageType.Info);
                EditorGUILayout.PropertyField(targetGameObjectProp, new GUIContent("Target GameObject (Override)"));

                EditorGUILayout.Space(4);
                DrawStateBoxes("Target Active", "Target Active");
                break;

            case ToggleTargetType.Component:
                DrawSectionHeader(EditorGUIUtility.IconContent("d_ScriptableObject Icon"), "Target");
                DrawComponentSelector(marker);

                EditorGUILayout.Space(4);
                DrawStateBoxes("Target Enabled", "Target Enabled");
                break;

            case ToggleTargetType.UdonEvent:
                DrawSectionHeader(EditorGUIUtility.IconContent("d_cs Script Icon"), "Target");
                DrawTargetField(targetUdonProp, marker.gameObject, typeof(UdonBehaviour), "Target Udon Behaviour");

                EditorGUILayout.Space(4);
                DrawEventBoxes();
                break;

            case ToggleTargetType.OtherToggle:
                DrawSectionHeader(EditorGUIUtility.IconContent("d_Toggle Icon"), "Target");
                DrawTargetField(targetSimpleToggleProp, marker.gameObject, typeof(SimpleUdonToggle), "Target Simple Toggle");

                EditorGUILayout.Space(4);
                DrawStateBoxes("Set Target ON", "Set Target ON");
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSectionHeader(GUIContent icon, string title)
    {
        EditorGUILayout.BeginHorizontal();

        if (icon != null)
        {
            GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(18));
        }

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

    private void DrawStateBoxes(string onLabel, string offLabel)
    {
        Color previousBackgroundColor = GUI.backgroundColor;

        GUI.backgroundColor = new Color(0.2f, 0.6f, 0.2f, 0.3f);
        EditorGUILayout.BeginVertical(_onSectionStyle);
        GUI.backgroundColor = previousBackgroundColor;
        DrawStateHeader(_onIcon, "When Toggle is ON", new Color(0.3f, 0.7f, 0.3f));
        EditorGUILayout.PropertyField(stateWhenOnProp, new GUIContent(onLabel));
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(4);

        GUI.backgroundColor = new Color(0.6f, 0.2f, 0.2f, 0.3f);
        EditorGUILayout.BeginVertical(_offSectionStyle);
        GUI.backgroundColor = previousBackgroundColor;
        DrawStateHeader(_offIcon, "When Toggle is OFF", new Color(0.7f, 0.3f, 0.3f));
        EditorGUILayout.PropertyField(stateWhenOffProp, new GUIContent(offLabel));
        EditorGUILayout.EndVertical();
    }

    private void DrawEventBoxes()
    {
        Color previousBackgroundColor = GUI.backgroundColor;

        GUI.backgroundColor = new Color(0.2f, 0.6f, 0.2f, 0.3f);
        EditorGUILayout.BeginVertical(_onSectionStyle);
        GUI.backgroundColor = previousBackgroundColor;
        DrawStateHeader(_onIcon, "When Toggle is ON", new Color(0.3f, 0.7f, 0.3f));
        EditorGUILayout.PropertyField(eventNameOnProp, new GUIContent("Event Name"));
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(4);

        GUI.backgroundColor = new Color(0.6f, 0.2f, 0.2f, 0.3f);
        EditorGUILayout.BeginVertical(_offSectionStyle);
        GUI.backgroundColor = previousBackgroundColor;
        DrawStateHeader(_offIcon, "When Toggle is OFF", new Color(0.7f, 0.3f, 0.3f));
        EditorGUILayout.PropertyField(eventNameOffProp, new GUIContent("Event Name"));
        EditorGUILayout.EndVertical();
    }

    private void DrawTargetField(SerializedProperty prop, GameObject owner, System.Type type, string label)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(prop, new GUIContent(label));
        
        // Auto-fix button if null but component exists on self
        if (prop.objectReferenceValue == null)
        {
            Component c = owner.GetComponent(type);
            if (c != null)
            {
                if (GUILayout.Button("Use Self", GUILayout.Width(60)))
                {
                    prop.objectReferenceValue = c;
                }
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawComponentSelector(SimpleToggleMarker marker)
    {
        // Draw the raw property first (allows dragging things in)
        EditorGUILayout.PropertyField(targetComponentProp, new GUIContent("Target Component"));
        
        // Show dropdown of components on the GameObject
        Component[] components = marker.GetComponents<Component>();
        List<string> names = new List<string>();
        List<Component> validComps = new List<Component>();
        
        // Add "Select from Self..." option which acts as current selection or placeholder
        // Actually, let's just show available components.
        
        names.Add("Select from Self..."); // Index 0
        validComps.Add(null); 

        foreach (var c in components)
        {
            if (c == null) continue;
            if (c == marker) continue; // Skip the marker itself
            if (c is Transform) continue; // Skip Transform (cannot be disabled)

            string typeName = c.GetType().Name;
            // Handle duplicates by counting existing entries of same type in validComps
            int count = 0;
            foreach (var vc in validComps)
            {
                if (vc != null && vc.GetType() == c.GetType()) count++;
            }
            if (count > 0) typeName += $" ({count})";
            
            names.Add(typeName);
            validComps.Add(c);
        }

        if (validComps.Count > 1) // More than just the placeholder
        {
            // Try to find current selection
            int currentIndex = 0;
            if (targetComponentProp.objectReferenceValue != null)
            {
                currentIndex = validComps.IndexOf(targetComponentProp.objectReferenceValue as Component);
                if (currentIndex == -1) currentIndex = 0; // Not in list (maybe on another object)
            }

            int newIndex = EditorGUILayout.Popup("Quick Select", currentIndex, names.ToArray());
            
            if (newIndex > 0) // Did not select placeholder
            {
                targetComponentProp.objectReferenceValue = validComps[newIndex];
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No valid components found on this GameObject (besides Transform/Marker).", MessageType.Warning);
        }
    }
}
