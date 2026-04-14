using UnityEngine;
using System.Collections;

public class NPCController2 : MonoBehaviour
{
    public Animator animator;
    public DeliveryZone deliveryZone;
    public CashRegister cashRegister;
    public CardReader cardReader;
    public Customer customer;
    public GazeInteractor gazeInteractor;
    public float turnSpeed = 120f;

    private Quaternion targetRotation;
    private bool isTurning = false;
    private bool turnTriggered = false;

    void Update()
    {
        animator.SetBool("CorrectItem2", deliveryZone.correctItem);
        animator.SetBool("CashTaken2", (cashRegister.cashTaken || cardReader.cardTaken));
        animator.SetBool("notlooking", gazeInteractor.isLookedAt);

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

        // Reset CorrectItem once transition away from Standing Idle 0 0 has started
        if (animator.IsInTransition(0) && deliveryZone.correctItem)
        {
            deliveryZone.correctItem = false;
            animator.SetBool("CorrectItem2", false);
        }

        // Reset CashTaken once Picking Up Object 0 starts
        if (state.IsName("Picking Up Object 0") && (cashRegister.cashTaken || cardReader.cardTaken))
        {
            cashRegister.cashTaken = false;
            cardReader.cardTaken = false;
            animator.SetBool("CashTaken2", false);
        }

        if (isTurning)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );
            if (Quaternion.Angle(transform.rotation, targetRotation) < 0.1f)
            {
                transform.rotation = targetRotation;
                isTurning = false;
            }
        }

        if (state.IsName("Walking") && state.normalizedTime >= 1f && !animator.IsInTransition(0))
            customer.onCashier();

        if (!turnTriggered && state.IsName("Picking Up Object 0") && state.normalizedTime >= 0.8f)
        {
            turnTriggered = true;
            targetRotation = transform.rotation * Quaternion.Euler(0, -180f, 0);
            isTurning = true;
        }

        if (state.IsName("Walking 1") && state.normalizedTime >= 1f && !animator.IsInTransition(0))
            StartCoroutine(DeactivateAfterDelay(4f));
        }

    IEnumerator DeactivateAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }
}