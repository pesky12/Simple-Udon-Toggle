using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VRC.Udon;

[CustomEditor(typeof(SimpleToggleMarker))]
public class SimpleToggleMarkerEditor : Editor
{
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

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        SimpleToggleMarker marker = (SimpleToggleMarker)target;

        EditorGUILayout.PropertyField(toggleNameProp);
        EditorGUILayout.PropertyField(targetTypeProp);
        EditorGUILayout.Space();

        ToggleTargetType type = (ToggleTargetType)targetTypeProp.enumValueIndex;

        switch (type)
        {
            case ToggleTargetType.UiToggle:
                DrawTargetField(targetUiToggleProp, marker.gameObject, typeof(Toggle), "Target UI Toggle");
                EditorGUILayout.HelpBox("This marker will treat the UI Toggle as the controller/display for the SimpleUdonToggle.", MessageType.Info);
                break;

            case ToggleTargetType.GameObject:
                // For GameObject, we usually target the one the marker is on, but allow override
                EditorGUILayout.HelpBox("Target GameObject defaults to THIS object if left empty.", MessageType.Info);
                EditorGUILayout.PropertyField(targetGameObjectProp, new GUIContent("Target GameObject (Override)"));
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Desired Active State", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(stateWhenOnProp, new GUIContent("Active When ON"));
                EditorGUILayout.PropertyField(stateWhenOffProp, new GUIContent("Active When OFF"));
                break;

            case ToggleTargetType.Component:
                DrawComponentSelector(marker);
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Desired Enabled State", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(stateWhenOnProp, new GUIContent("Enabled When ON"));
                EditorGUILayout.PropertyField(stateWhenOffProp, new GUIContent("Enabled When OFF"));
                break;

            case ToggleTargetType.UdonEvent:
                DrawTargetField(targetUdonProp, marker.gameObject, typeof(UdonBehaviour), "Target Udon Behaviour");
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Events to Send", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(eventNameOnProp, new GUIContent("Event When ON"));
                EditorGUILayout.PropertyField(eventNameOffProp, new GUIContent("Event When OFF"));
                break;

            case ToggleTargetType.OtherToggle:
                DrawTargetField(targetSimpleToggleProp, marker.gameObject, typeof(SimpleUdonToggle), "Target Simple Toggle");
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Desired State", EditorStyles.boldLabel);
                // Maybe rename labels for clarity
                EditorGUILayout.PropertyField(stateWhenOnProp, new GUIContent("Sync State When ON"));
                EditorGUILayout.PropertyField(stateWhenOffProp, new GUIContent("Sync State When OFF"));
                break;
        }

        serializedObject.ApplyModifiedProperties();
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
