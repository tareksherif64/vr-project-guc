using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Attach to each Pin sphere. Grab and drag to another pin to draw a string.
/// On release, snaps to the nearest pin within threshold distance.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(XRSimpleInteractable))]
public class PinConnector : MonoBehaviour
{
    public EvidenceBoardManager boardManager;

    [Tooltip("How close (metres) the hand must be to a pin when releasing to snap to it.")]
    public float snapRadius = 0.25f;

    // ── shared drawing state ──────────────────────────────────────────
    private static PinConnector       _sourcePin;
    private static Transform          _interactorTf;
    private static EvidenceStringLine _preview;

    private XRSimpleInteractable _xri;

    void Awake()
    {
        _xri = GetComponent<XRSimpleInteractable>();

        // Make the collider large enough to hit comfortably in world space.
        // The pin inherits its parent slot's tiny scale, so we compute the
        // local radius that produces a ~5 cm world-space sphere.
        var col = GetComponent<SphereCollider>();
        float worldMax = Mathf.Max(transform.lossyScale.x,
                                   transform.lossyScale.y,
                                   transform.lossyScale.z);
        col.radius = 0.05f / Mathf.Max(worldMax, 0.0001f);
    }

    void OnEnable()
    {
        _xri.selectEntered.AddListener(OnGrab);
        _xri.selectExited.AddListener(OnRelease);
    }

    void OnDisable()
    {
        _xri.selectEntered.RemoveListener(OnGrab);
        _xri.selectExited.RemoveListener(OnRelease);
    }

    void Update()
    {
        if (_sourcePin != this || _preview == null || _interactorTf == null) return;

        // Snap preview to nearest pin in range, otherwise follow the hand
        var nearest = FindNearest(_interactorTf.position, snapRadius);
        var end = (nearest != null) ? nearest.transform.position : _interactorTf.position;
        _preview.SetPoints(transform.position, end);
    }

    // ── grab: start drawing ───────────────────────────────────────────

    private void OnGrab(SelectEnterEventArgs args)
    {
        if (_sourcePin != null) return;

        _sourcePin    = this;
        _interactorTf = args.interactorObject.transform;

        var go = new GameObject("PreviewLine");
        _preview = go.AddComponent<EvidenceStringLine>();
        _preview.SetPoints(transform.position, transform.position);
    }

    // ── release: commit or cancel ─────────────────────────────────────

    private void OnRelease(SelectExitEventArgs args)
    {
        if (_sourcePin != this) return;

        var nearest = FindNearest(_interactorTf.position, snapRadius);
        if (nearest != null)
            boardManager?.CreateLine(transform.position, nearest.transform.position);

        if (_preview != null) { Destroy(_preview.gameObject); _preview = null; }
        _sourcePin    = null;
        _interactorTf = null;
    }

    // ── helpers ───────────────────────────────────────────────────────

    private PinConnector FindNearest(Vector3 worldPos, float radius)
    {
        PinConnector best = null;
        float bestDist = radius;

        foreach (var pin in FindObjectsByType<PinConnector>(FindObjectsSortMode.None))
        {
            if (pin == this) continue;
            float d = Vector3.Distance(worldPos, pin.transform.position);
            if (d < bestDist) { bestDist = d; best = pin; }
        }
        return best;
    }
}
