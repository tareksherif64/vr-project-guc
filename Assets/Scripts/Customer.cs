using UnityEngine;
using UnityEngine.AI;
using TMPro;

public class Customer : MonoBehaviour
{
    public enum ItemType { Cupcake, Croissant, Espresso }

    public ItemType OrderItem;

    public Transform cashierDestination;
    public Transform exitPoint;
    public Player player;
    public CustomerManager manager;

    // Kept to avoid breaking scene serialisation — no longer used at runtime
    [HideInInspector] public GameObject VisaMachine;
    [HideInInspector] public GameObject Cash;

    public TextMeshProUGUI customerText;
    public DeliveryZone deliveryZone;

    private NavMeshAgent agent;
    private bool atCashier = false;

    void Awake() => agent = GetComponent<NavMeshAgent>();

    public void StartCustomer()
    {
        atCashier = false;
        gameObject.SetActive(true);

    }

    public void onCashier()
    {
        atCashier = true;
        player.SetCurrentCustomer(this);
        if (deliveryZone != null)
            deliveryZone.activeCustomer = this;
    }

    public void ReceiveDrink()
    {
        player.CorrectOrder();
    }

    /// <summary>Called by CashRegister or CardReader when physical payment is done.</summary>
    public void CompletePayment()
    {
        
        LeaveCafe();
    }

    public void LeaveCafe()
    {
        atCashier = false;
        if (deliveryZone != null)
            deliveryZone.activeCustomer = null;
        manager.NextCustomer();
    }
}
