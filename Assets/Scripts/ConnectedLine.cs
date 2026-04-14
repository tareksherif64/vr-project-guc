using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// A permanent string between two pins.
/// Has a dotted indicator at the center and a grab handle to delete it.
/// </summary>
public class ConnectedLine : MonoBehaviour
{
    public void Initialize(Vector3 from, Vector3 to)
    {
        // Main string
        var line = gameObject.AddComponent<EvidenceStringLine>();
        line.SetPoints(from, to);

        // Dotted cut indicator across the middle third of the line
        AddDots(from, to);

        // Delete handle at midpoint — smaller collider than before
        var mid = (from + to) * 0.5f;
        var handle = new GameObject("DeleteHandle");
        handle.transform.position = mid;
        handle.transform.SetParent(transform, true);

        var col = handle.AddComponent<SphereCollider>();
        col.radius = 0.02f;

        var xri = handle.AddComponent<XRSimpleInteractable>();
        xri.selectEntered.AddListener(_ => Destroy(gameObject));
    }

    private void AddDots(Vector3 from, Vector3 to)
    {
        // 7 small bright dots from 35% to 65% along the line
        const int dotCount = 7;
        const float rangeStart = 0.35f;
        const float rangeEnd   = 0.65f;

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", new Color(1f, 0.95f, 0.7f)); // warm white
        mat.SetFloat("_Metallic", 0f);
        mat.SetFloat("_Smoothness", 0.8f);

        for (int i = 0; i < dotCount; i++)
        {
            float t = Mathf.Lerp(rangeStart, rangeEnd, (float)i / (dotCount - 1));
            var pos = Vector3.Lerp(from, to, t);

            var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dot.name = "CutDot";
            dot.transform.position = pos;
            dot.transform.localScale = Vector3.one * 0.008f;
            dot.transform.SetParent(transform, true);

            Destroy(dot.GetComponent<Collider>());
            dot.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }
}
