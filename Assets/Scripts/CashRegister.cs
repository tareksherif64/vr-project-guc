using UnityEngine;

/// <summary>
/// Trigger zone on the cash register/till.
/// Drop a CashItem into it to complete a cash payment.
/// </summary>
public class CashRegister : MonoBehaviour
{
    [HideInInspector] public Customer activeCustomer;
    public bool cashTaken = false;
    void OnTriggerEnter(Collider other)
    {
        if (activeCustomer == null) return;

        var cash = other.GetComponent<CashItem>();
        if (cash == null) return;

        var customer = activeCustomer;
        activeCustomer = null;
        cashTaken = true;
        cash.ReturnHome();
        customer.CompletePayment();
    }
}
