using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Attach to the Scissors root alongside XRGrabInteractable.
/// While held, the moving blade rotates by an amount proportional
/// to how hard the player squeezes the grab button (0 = closed, 1 = fully open).
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class ScissorsGrab : MonoBehaviour
{
    [Header("Blade")]
    [Tooltip("The child transform that should rotate when squeezing.")]
    public Transform movingBlade;

    [Tooltip("Local-space axis around which the blade rotates.")]
    public Vector3 rotationAxis = Vector3.up;

    [Tooltip("Angle (degrees) when grip is fully released.")]
    public float minAngle = 0f;

    [Tooltip("Angle (degrees) when grip is fully pressed.")]
    public float maxAngle = 35f;

    private XRGrabInteractable _grab;
    private IXRSelectInteractor _interactor;
    private XRNode _handNode;
    private Quaternion _baseRotation;

    void Awake()
    {
        _grab = GetComponent<XRGrabInteractable>();
        if (movingBlade != null)
            _baseRotation = movingBlade.localRotation;
    }

    void OnEnable()
    {
        _grab.selectEntered.AddListener(OnGrab);
        _grab.selectExited.AddListener(OnRelease);
    }

    void OnDisable()
    {
        _grab.selectEntered.RemoveListener(OnGrab);
        _grab.selectExited.RemoveListener(OnRelease);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        _interactor = args.interactorObject;
        // Determine which hand is holding by checking the interactor's GO name
        var name = (args.interactorObject as MonoBehaviour)?.gameObject.name ?? "";
        _handNode = name.ToLower().Contains("left") ? XRNode.LeftHand : XRNode.RightHand;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        _interactor = null;
    }

    void Update()
    {
        if (_interactor == null || movingBlade == null) return;

        var device = InputDevices.GetDeviceAtXRNode(_handNode);
        device.TryGetFeatureValue(CommonUsages.grip, out float grip);

        float angle = Mathf.Lerp(minAngle, maxAngle, grip);
        movingBlade.localRotation = _baseRotation * Quaternion.AngleAxis(angle, rotationAxis);
    }
}
