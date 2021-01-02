using UnityEngine;
using UnityEngine.Events;

public class InteractableBase : MonoBehaviour
{
    public GameObject interactionCauser { get; private set; }
    public float radius = 3f;
    public UnityAction onInteract;
    public int maxInteractions = 1;

    private int interactionCount = 0;

    public void Interact(GameObject character)
    {
        if (interactionCount < maxInteractions) {
            interactionCount +=1;
            interactionCauser = character;

            if (onInteract != null)
            {
                onInteract.Invoke();
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
