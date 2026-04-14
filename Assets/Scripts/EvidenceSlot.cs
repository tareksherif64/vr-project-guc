using UnityEngine;

// Data container only — XR interaction is handled by the child Pin's PinConnector.
public class EvidenceSlot : MonoBehaviour
{
    public int slotIndex;
    public EvidenceBoardManager boardManager;
    public Transform pinTransform;
}
