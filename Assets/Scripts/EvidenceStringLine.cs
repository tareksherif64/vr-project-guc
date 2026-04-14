using UnityEngine;

/// <summary>
/// A single string (LineRenderer) drawn from one world position to another.
/// Spawned by EvidenceBoardManager to connect the player spawn to clicked slots.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class EvidenceStringLine : MonoBehaviour
{
    private LineRenderer _lr;

    void Awake()
    {
        _lr = GetComponent<LineRenderer>();
        _lr.positionCount = 2;
        _lr.useWorldSpace = true;

        // Red string look
        _lr.startWidth  = 0.004f;
        _lr.endWidth    = 0.004f;
        _lr.numCapVertices = 4;

        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = new Color(0.75f, 0.08f, 0.08f); // deep red
        _lr.material = mat;

        _lr.startColor = new Color(0.75f, 0.08f, 0.08f);
        _lr.endColor   = new Color(0.75f, 0.08f, 0.08f);
    }

    /// <summary>Set both endpoints in world space.</summary>
    public void SetPoints(Vector3 from, Vector3 to)
    {
        _lr.SetPosition(0, from);
        _lr.SetPosition(1, to);
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
