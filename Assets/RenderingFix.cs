using UnityEngine;

public class XRPerformanceFix : MonoBehaviour
{
    void Start()
    {
        Application.targetFrameRate = 72;
        QualitySettings.vSyncCount = 0;
    }
}