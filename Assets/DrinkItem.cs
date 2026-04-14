using System.Collections;
using UnityEngine;

public class DrinkItem : MonoBehaviour
{
    public Customer.ItemType itemType;
    public float price = 5.00f;

    private Vector3 homePosition;
    private Quaternion homeRotation;

    void Start()
    {
        homePosition = transform.position;
        homeRotation = transform.rotation;
    }

    public void ReturnHome()
    {
        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        gameObject.SetActive(false);
        yield return new WaitForSeconds(4f);
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        transform.SetPositionAndRotation(homePosition, homeRotation);
        gameObject.SetActive(true);
    }
}
