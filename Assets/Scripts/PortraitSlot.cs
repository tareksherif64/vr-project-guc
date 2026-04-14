using UnityEngine;

// Data container only — XR interaction is handled by the child Pin's PinConnector.
public class PortraitSlot : MonoBehaviour
{
    public int portraitIndex;
    public EvidenceBoardManager boardManager;
    public Transform pinTransform;
}
