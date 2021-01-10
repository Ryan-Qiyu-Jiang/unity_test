using System.Collections.Generic;
using UnityEngine;

public class DamageArea : MonoBehaviour
{
    [Tooltip("Area of damage when the projectile hits something")]
    public float areaOfEffectDistance = 5f;
    [Tooltip("Damage multiplier over distance for area of effect")]
    public AnimationCurve damageRatioOverDistance;

    [Header("Debug")]
    [Tooltip("Color of the area of effect radius")]
    public Color areaOfEffectColor = Color.red * 0.5f;

    public void InflictDamageInArea(float damage, Vector3 center, LayerMask layers, QueryTriggerInteraction interaction, GameObject owner)
    {
        HashSet<InteractionDamageSelf> uniqueDamageables = new HashSet<InteractionDamageSelf>();

        // Create a collection of unique health components that would be damaged in the area of effect (in order to avoid damaging a same entity multiple times)
        Collider[] affectedColliders = Physics.OverlapSphere(center, areaOfEffectDistance, layers, interaction);
        foreach (var coll in affectedColliders)
        {
            InteractionDamageSelf damageable = coll.GetComponent<InteractionDamageSelf>();
            if (damageable)
            {
                if (uniqueDamageables.Contains(damageable))
                {
                    uniqueDamageables.Add(damageable);
                }
            }
        }

        // Apply damages with distance falloff
        foreach (InteractionDamageSelf uniqueDamageable in uniqueDamageables)
        {
            float distance = Vector3.Distance(uniqueDamageable.transform.position, transform.position);
            uniqueDamageable.iteractiableBase.Interact(owner);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = areaOfEffectColor;
        Gizmos.DrawSphere(transform.position, areaOfEffectDistance);
    }
}
