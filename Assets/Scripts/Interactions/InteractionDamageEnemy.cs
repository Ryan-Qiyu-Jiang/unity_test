using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(InteractableBase))]
public class InteractionDamageEnemy : MonoBehaviour
{
    [Tooltip("Amount of damage to do to enemy")]
    public float damageAmount = 1f;
    InteractableBase m_IteractiableBase;
    CharacterStatsController m_SelfStatsController;
    private void OnEnable()
    {
        m_IteractiableBase = GetComponent<InteractableBase>();
        DebugUtility.HandleErrorIfNullGetComponent<InteractableBase, InteractionDamageEnemy>(m_IteractiableBase, this, gameObject);

        m_SelfStatsController = GetComponent<CharacterStatsController>();
        DebugUtility.HandleErrorIfNullGetComponent<CharacterStatsController, InteractionDamageEnemy>(m_SelfStatsController, this, gameObject);

        m_IteractiableBase.onInteract += OnInteract;
    }

    void OnInteract()
    {
        CharacterStatsController characterStatsController = m_IteractiableBase.interactionCauser.GetComponent<CharacterStatsController>();
        float damage = damageAmount * characterStatsController.ap.GetValue();
        m_SelfStatsController.TakeDamage(damage);        
        print(string.Format("{0} does {1} damage to {2}", m_IteractiableBase.interactionCauser.name, damage, gameObject.name));
    }
}
