using UnityEngine;

// Place this on a trigger collider at the customer's side of the counter.
// The CustomerManager or Customer sets activeCustomer when a customer arrives.
public class DeliveryZone : MonoBehaviour
{
    public Customer activeCustomer;
    public bool correctItem = false;
    void OnTriggerEnter(Collider other)
    {
        if (activeCustomer == null) return;

        var drink = other.GetComponent<DrinkItem>();
        if (drink == null) return;

        if (drink.itemType == activeCustomer.OrderItem)
        {
            correctItem = true;
            activeCustomer.ReceiveDrink();
            drink.ReturnHome();
        }
        else
        {
            // Wrong item — bounce it back toward the player side
            var rb = other.GetComponent<Rigidbody>();
            if (rb != null)
                rb.AddForce(-transform.forward * 3f, ForceMode.Impulse);
        }
    }
}
