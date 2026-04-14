using UnityEngine;

public class GazeInteractor : MonoBehaviour
{
    public Camera playerCamera;
    public bool isLookedAt = false;

    void Update()
    {
        Vector3 directionToObject = (transform.position - playerCamera.transform.position).normalized;
        float dot = Vector3.Dot(playerCamera.transform.forward, directionToObject);

        if (dot > 0.5f) // 0.5 = roughly 60 degree cone, adjust to taste
        {
            isLookedAt = true;
        }
        else if (isLookedAt)
        {
            isLookedAt = false;
        }
    }
}