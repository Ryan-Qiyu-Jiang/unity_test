using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(InteractableBase))]
public class InteractionSkillPickup : MonoBehaviour
{
    [Tooltip("Spell to learn")]
    public SpellController skillItem;
    [Tooltip("Text popup")]
    public GameObject textBox;
    InteractableBase m_IteractiableBase;
    PlayerSpellsManager m_PlayerSpellsManager;
    PlayerCharacterController m_PlayerCharacterController;
    PlayerInputHandler m_PlayerInputHandler;

    bool waitingForSpellBinding = false;
    private void OnEnable()
    {
        m_IteractiableBase = GetComponent<InteractableBase>();
        DebugUtility.HandleErrorIfNullGetComponent<InteractableBase, InteractionSkillPickup>(m_IteractiableBase, this, gameObject);

        m_IteractiableBase.onInteract += OnInteract;
    }

    void Update()
    {
        if (waitingForSpellBinding) {
            int spellBinding = GetSpellInputDown();
            if (spellBinding >= 0) {
                m_PlayerSpellsManager.SwitchToSpellIndex(skillItem, spellBinding);
                m_PlayerCharacterController.paused = false;
                textBox.SetActive(false);
                Destroy(gameObject, 0);
            }
        }
    }

    int GetSpellInputDown() {
        if (m_PlayerInputHandler.GetFireInputDown()) {
            return 0;
        }
        if (m_PlayerInputHandler.GetAltInputDown()) {
            return 1;
        }
        if (m_PlayerInputHandler.GetUtilityInputDown()) {
            return 2;
        }
        if (m_PlayerInputHandler.GetUltimateInputDown()) {
            return 3;
        }
        return -1;
    }
    void OnInteract()
    {
        m_PlayerSpellsManager = m_IteractiableBase.interactionCauser.GetComponent<PlayerSpellsManager>();
        m_PlayerCharacterController = m_IteractiableBase.interactionCauser.GetComponent<PlayerCharacterController>();
        m_PlayerInputHandler = m_IteractiableBase.interactionCauser.GetComponent<PlayerInputHandler>();

        m_PlayerCharacterController.paused = true;
        waitingForSpellBinding = true;
        textBox.SetActive(true);
    }
}
