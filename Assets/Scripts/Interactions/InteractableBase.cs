using UnityEngine;
using UnityEngine.Events;

public class InteractableBase : MonoBehaviour
{
    public float radius = 3f;
    public UnityAction<GameObject> onInteract;
    public int maxInteractions = 1;

    private int interactionCount = 0;

    public void Interact(GameObject caller)
    {
        if (interactionCount < maxInteractions) {
            interactionCount +=1;

            if (onInteract != null)
            {
                onInteract.Invoke(caller);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
