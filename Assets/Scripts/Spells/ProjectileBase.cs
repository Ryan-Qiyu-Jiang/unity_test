using UnityEngine;
using UnityEngine.Events;

public class ProjectileBase : MonoBehaviour
{
    public float spellDamageModifier { get; private set; }
    public GameObject owner { get; private set; }
    public Vector3 initialPosition { get; private set; }
    public Vector3 initialDirection { get; private set; }
    public Vector3 inheritedMuzzleVelocity { get; private set; }
    public float initialCharge { get; private set; }

    public UnityAction onShoot;

    public void Shoot(SpellController controller)
    {
        owner = controller.owner;
        initialPosition = transform.position;
        initialDirection = transform.forward;
        inheritedMuzzleVelocity = controller.muzzleWorldVelocity;
        initialCharge = controller.currentCharge;
        spellDamageModifier = controller.spellDamageModifier;

        if (onShoot != null)
        {
            onShoot.Invoke();
        }
    }
}