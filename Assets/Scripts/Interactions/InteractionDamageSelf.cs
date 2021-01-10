using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(InteractableBase))]
public class InteractionDamageSelf : MonoBehaviour
{
    [Tooltip("Amount of damage to do to enemy")]
    public float selfDamageModifier = 1f;
    public InteractableBase iteractiableBase { get; private set; }
    CharacterStatsController m_SelfStatsController;
    private void OnEnable()
    {
        iteractiableBase = GetComponent<InteractableBase>();
        DebugUtility.HandleErrorIfNullGetComponent<InteractableBase, InteractionDamageSelf>(iteractiableBase, this, gameObject);

        m_SelfStatsController = GetComponent<CharacterStatsController>();
        DebugUtility.HandleErrorIfNullGetComponent<CharacterStatsController, InteractionDamageSelf>(m_SelfStatsController, this, gameObject);

        iteractiableBase.onInteract += OnInteract;
    }

    void OnInteract(GameObject caller)
    {
        ProjectileBase projectileBase = caller.GetComponent<ProjectileBase>();
        float damage;
        if (projectileBase != null) {
            // if interact from spell
            CharacterStatsController characterStatsController = projectileBase.owner.GetComponent<CharacterStatsController>();
            float spellDamageModifier = projectileBase.spellDamageModifier;
            damage = selfDamageModifier * spellDamageModifier * characterStatsController.ap.GetValue();
        } else {
            // if interact directly
            CharacterStatsController characterStatsController = caller.GetComponent<CharacterStatsController>();
            damage = selfDamageModifier * characterStatsController.ap.GetValue();
        }
        m_SelfStatsController.TakeDamage(damage);
        // print(string.Format("{0} does {1} damage to {2}", caller.name, damage, gameObject.name));
    }
}
