using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ReplayManager : MonoBehaviour
{
    public static ReplayManager Instance;

    private class Recording
    {
        public List<ReplayRecorder.Frame> frames;
        public List<ReplayRecorder.AnimationEvent> animEvents;
        public GameObject ghostPrefab;
    }

    private Dictionary<string, Recording> recordings = new Dictionary<string, Recording>();
    private List<Coroutine> activeReplays = new List<Coroutine>();
    private List<GameObject> activeGhosts = new List<GameObject>();
    private bool replaying = false;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SaveRecording(string id, List<ReplayRecorder.Frame> frames, List<ReplayRecorder.AnimationEvent> animEvents, GameObject ghostPrefab)
    {
        recordings[id] = new Recording { frames = frames, animEvents = animEvents, ghostPrefab = ghostPrefab };
        Debug.Log($"[ReplayManager] Saved {id} | Frames: {frames.Count} | AnimEvents: {animEvents.Count}");
    }

    public void SaveRecording(string id, List<ReplayRecorder2.Frame> frames, List<ReplayRecorder2.AnimationEvent> animEvents, GameObject ghostPrefab)
    {
        var convertedFrames = new List<ReplayRecorder.Frame>();
        foreach (var f in frames)
            convertedFrames.Add(new ReplayRecorder.Frame { position = f.position, rotation = f.rotation });

        var convertedEvents = new List<ReplayRecorder.AnimationEvent>();
        foreach (var e in animEvents)
            convertedEvents.Add(new ReplayRecorder.AnimationEvent { timestamp = e.timestamp, stateName = e.stateName });

        recordings[id] = new Recording { frames = convertedFrames, animEvents = convertedEvents, ghostPrefab = ghostPrefab };
        Debug.Log($"[ReplayManager] Saved {id} | Frames: {frames.Count} | AnimEvents: {animEvents.Count}");
    }

    public void PlayAll(System.Action onComplete = null)
    {
        if (replaying) return;
        StartCoroutine(PlayAllRoutine(onComplete));
    }

    IEnumerator PlayAllRoutine(System.Action onComplete)
    {
        replaying = true;
        activeReplays.Clear();
        activeGhosts.Clear();

        foreach (var kvp in recordings)
            activeReplays.Add(StartCoroutine(RunReplay(kvp.Key, kvp.Value)));

        foreach (var c in activeReplays)
            yield return c;

        replaying = false;
        onComplete?.Invoke();
    }

    IEnumerator RunReplay(string id, Recording rec)
    {
        if (rec.frames.Count == 0) yield break;

        GameObject ghost = Instantiate(rec.ghostPrefab,
                           rec.frames[0].position,
                           rec.frames[0].rotation);

        Animator ghostAnim = ghost.GetComponent<Animator>();
        if (ghostAnim != null) ghostAnim.enabled = false;

        activeGhosts.Add(ghost);

        float startTime = Time.time;
        int eventIndex = 0;

        foreach (var frame in rec.frames)
        {
            ghost.transform.position = frame.position;
            ghost.transform.rotation = frame.rotation;

            float elapsed = Time.time - startTime;
            while (eventIndex < rec.animEvents.Count &&
                   elapsed >= rec.animEvents[eventIndex].timestamp)
            {
                if (ghostAnim != null)
                {
                    ghostAnim.enabled = true;
                    ghostAnim.Play(rec.animEvents[eventIndex].stateName, 0, 0f);
                    Debug.Log($"[ReplayManager] Playing: {rec.animEvents[eventIndex].stateName}");
                }
                eventIndex++;
            }

            yield return null;
        }

        activeGhosts.Remove(ghost);
        Destroy(ghost);
        Debug.Log($"[ReplayManager] Finished replay for {id}");
    }

    public void StopAll()
    {
        foreach (var c in activeReplays) StopCoroutine(c);
        foreach (var g in activeGhosts) if (g != null) Destroy(g);
        activeReplays.Clear();
        activeGhosts.Clear();
        replaying = false;
    }

    public bool IsReplaying => replaying;
}