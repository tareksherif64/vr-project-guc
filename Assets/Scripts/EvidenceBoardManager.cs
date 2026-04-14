using UnityEngine;

public class EvidenceBoardManager : MonoBehaviour
{
    public void CreateLine(Vector3 from, Vector3 to)
    {
        var go = new GameObject("ConnectedLine");
        go.transform.SetParent(transform, false);
        go.AddComponent<ConnectedLine>().Initialize(from, to);
    }
}
