using UnityEngine;

public class PushRigidbodies : MonoBehaviour
{
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody rb = hit.collider.attachedRigidbody;

        if (rb != null && !rb.isKinematic)
        {
            Vector3 forceDir = hit.moveDirection;
            forceDir.y = 0;
            rb.AddForce(forceDir * 2f, ForceMode.Impulse);
        }
    }
}
