using UnityEngine;

/// <summary>
/// Marks the physical cash grab object. Resets physics and returns home after use.
/// </summary>
public class CashItem : MonoBehaviour
{
    private Vector3 homePosition;
    private Quaternion homeRotation;

    void Start()
    {
        homePosition = transform.position;
        homeRotation = transform.rotation;
    }

    public void ReturnHome()
    {
        // Reset velocity BEFORE deactivating — coroutines are killed on SetActive(false)
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity  = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        transform.SetPositionAndRotation(homePosition, homeRotation);
        gameObject.SetActive(false);
        // Player.PayWithCash() will reactivate it for the next payment
    }
}
