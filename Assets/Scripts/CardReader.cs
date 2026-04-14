using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Attach alongside an XRSocketInteractor on the card reader.
/// When a PaymentCard snaps into the socket, payment is completed.
/// </summary>
[RequireComponent(typeof(XRSocketInteractor))]
public class CardReader : MonoBehaviour
{
    public bool cardTaken = false;
    void Awake()
    {
        GetComponent<XRSocketInteractor>().selectEntered.AddListener(OnCardInserted);
    }

    void OnCardInserted(SelectEnterEventArgs args)
    {
        var card = args.interactableObject.transform.GetComponent<PaymentCard>();
        if (card == null || card.activeCustomer == null) return;

        var customer = card.activeCustomer;
        card.activeCustomer = null;

        StartCoroutine(CompleteAfterSnap(card, customer));
    }

    IEnumerator CompleteAfterSnap(PaymentCard card, Customer customer)
    {
        // Brief pause so the player sees the card snapped in before it disappears
        yield return new WaitForSeconds(1f);
        card.gameObject.SetActive(false);
        cardTaken = true;
        customer.CompletePayment();
    }
}
