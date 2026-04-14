using UnityEngine;

public class MonitorInteraction : MonoBehaviour
{
    public GameObject evidencePanelUI;

    public void OnMonitorSelected()
    {
        if (ReplayManager.Instance.IsReplaying) return;
        Debug.Log("[MonitorInteraction] Starting playback");
        ReplayManager.Instance.PlayAll(OnPlaybackComplete);
    }

    void OnPlaybackComplete()
    {
        Debug.Log("[MonitorInteraction] Playback done");
        if (evidencePanelUI != null)
            evidencePanelUI.SetActive(true);
    }
}