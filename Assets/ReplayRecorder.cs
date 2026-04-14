using UnityEngine;
using System.Collections.Generic;

public class ReplayRecorder : MonoBehaviour, IReplayRecorder
{
    [System.Serializable]
    public struct Frame
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    [System.Serializable]
    public struct AnimationEvent
    {
        public float timestamp;
        public string stateName;
    }

    public string actorID;
    public GameObject ghostPrefab;

    private Animator anim;
    private List<Frame> frames = new List<Frame>();
    private List<AnimationEvent> animEvents = new List<AnimationEvent>();
    private string lastStateName = "";
    private bool recording = false;
    private float recordingStartTime;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (!recording) return;

        frames.Add(new Frame
        {
            position = transform.position,
            rotation = transform.rotation
        });

        AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
        string currentState = GetStateName(state);

        if (currentState != lastStateName && currentState != "")
        {
            lastStateName = currentState;
            animEvents.Add(new AnimationEvent
            {
                timestamp = Time.time - recordingStartTime,
                stateName = currentState
            });
            Debug.Log($"[ReplayRecorder] Animation changed to: {currentState} at {Time.time - recordingStartTime}");
        }
    }

    string GetStateName(AnimatorStateInfo state)
    {
        string[] stateNames = new string[]
        {
            "Standing Idle",
            "Walking",
            "Standing Idle 0",
            "Giving Object",
            "Standing Idle 0 0",
            "Picking Up Object 0",
            "Walking 1"
        };

        foreach (var name in stateNames)
            if (state.IsName(name)) return name;

        return "";
    }

    public void StartRecording()
    {
        frames.Clear();
        animEvents.Clear();
        lastStateName = "";
        recordingStartTime = Time.time;
        recording = true;
    }

    public void StopAndSave()
    {
        if (!recording) return;
        recording = false;
        Debug.Log($"[ReplayRecorder] Stopping. Frames: {frames.Count} | AnimEvents: {animEvents.Count}");
        foreach (var e in animEvents)
            Debug.Log($"  -> {e.stateName} at {e.timestamp}");
        ReplayManager.Instance.SaveRecording(actorID, frames, animEvents, ghostPrefab);
    }
}