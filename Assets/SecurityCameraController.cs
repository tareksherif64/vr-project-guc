using UnityEngine;

public class SecurityCameraController : MonoBehaviour
{
    // Drag the SecurityMonitor's MeshRenderer here in the Inspector
    public MeshRenderer monitorRenderer;

    // Footage clips captured by this camera — assign Texture2D assets in the Inspector
    public Texture2D[] footageClips; // 0=customer1, 1=customer2, 2=customer3

    private int currentClip = 0;

    void Start()
    {
        // No live feed — disable the camera component entirely
        var cam = GetComponent<Camera>();
        if (cam != null) cam.enabled = false;

        // Show the first clip on startup if one exists
        if (footageClips != null && footageClips.Length > 0)
            ShowClip(0);
    }

    // Display a specific clip on the monitor
    public void ShowClip(int clipIndex)
    {
        if (footageClips == null || clipIndex < 0 || clipIndex >= footageClips.Length) return;
        currentClip = clipIndex;
        if (monitorRenderer != null)
            monitorRenderer.material.mainTexture = footageClips[clipIndex];
    }

    // Step forward through clips
    public void NextClip()
    {
        if (footageClips == null || footageClips.Length == 0) return;
        ShowClip((currentClip + 1) % footageClips.Length);
    }

    // Step backward through clips
    public void PreviousClip()
    {
        if (footageClips == null || footageClips.Length == 0) return;
        ShowClip((currentClip - 1 + footageClips.Length) % footageClips.Length);
    }

    public int CurrentClipIndex => currentClip;
    public int ClipCount => footageClips != null ? footageClips.Length : 0;
}
