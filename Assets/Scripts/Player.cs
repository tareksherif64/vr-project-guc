using TMPro;
using UnityEngine;

public class Player : MonoBehaviour
{
    public TextMeshProUGUI text;

    // Physical payment objects — assign in Inspector
    public CashItem     cashObject;    // the grabbable cash on the counter
    public CashRegister cashRegister;  // trigger zone inside the register/till
    public PaymentCard  paymentCard;   // the card quad that appears for card payments
    public CardReader   cardReader;    // the permanent Eftpos machine
    public Transform    cardSpawnPoint; // where the card appears — position this on the counter in front of the customer

    private Customer currentCustomer;

    public void SetCurrentCustomer(Customer customer)
    {
        currentCustomer = customer;
    }

    /// <summary>Called by DeliveryZone when the correct drink is handed over.</summary>
    public void CorrectOrder()
    {
        if (currentCustomer == null) return;

        bool isVisa = Random.Range(0, 2) == 1;

        if (isVisa)
            PayWithVisa();
        else
            PayWithCash();
    }

    void PayWithCash()
    {

        if (cashRegister != null)
            cashRegister.activeCustomer = currentCustomer;


        if (cashObject != null)
            cashObject.gameObject.SetActive(true);

        currentCustomer = null;
    }

    void PayWithVisa()
    {

        if (paymentCard != null)
        {
            // Use the dedicated spawn point if set, otherwise fall back to counter height above cashier trigger
            Vector3 spawnPos = cardSpawnPoint != null
                ? cardSpawnPoint.position
                : currentCustomer.cashierDestination.position + Vector3.up * 1.1f;
            Quaternion spawnRot = cardSpawnPoint != null ? cardSpawnPoint.rotation : Quaternion.identity;

            paymentCard.transform.SetPositionAndRotation(spawnPos, spawnRot);

            // Reset physics so it doesn't fly off
            var rb = paymentCard.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity  = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            paymentCard.activeCustomer = currentCustomer;
            paymentCard.gameObject.SetActive(true);
        }

        currentCustomer = null;
    }
}
