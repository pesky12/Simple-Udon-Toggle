using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

public enum ToggleTargetType
{
    UiToggle,
    GameObject,
    Component,
    UdonEvent,
    OtherToggle
}

/// <summary>
/// Editor-only marker component that identifies objects for auto-assignment to SimpleUdonToggle instances.
/// Place this on any GameObject and set the toggleName to match your SimpleUdonToggle's toggleName.
/// </summary>
public class SimpleToggleMarker : MonoBehaviour, IEditorOnly
{
    [Tooltip("The name of the SimpleUdonToggle this element should connect to")]
    public string toggleName;

    [Tooltip("What kind of connection this is")]
    public ToggleTargetType targetType = ToggleTargetType.UiToggle;

    [Tooltip("State or Value when toggle is ON")]
    public bool stateWhenOn = true;
    [Tooltip("State or Value when toggle is OFF")]
    public bool stateWhenOff = false;

    // Specific Targets (optional overrides - if null, defaults to GetComponent on self)
    [Tooltip("Component to enable/disable (e.g. Light, Camera, AudioSource)")]
    public Behaviour targetBehaviour;
    public UdonBehaviour targetUdon;
    public Toggle targetUiToggle;
    public SimpleUdonToggle targetSimpleToggle;
    public GameObject targetGameObject;

    [Tooltip("Event to send when toggle turns ON")]
    public string eventNameOn;
    [Tooltip("Event to send when toggle turns OFF")]
    public string eventNameOff;
}
