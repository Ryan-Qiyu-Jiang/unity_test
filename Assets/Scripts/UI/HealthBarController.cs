using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
public class HealthBarController : MonoBehaviour
{
    public Slider slider;
    public GameObject character;
    private CharacterStatsController m_CharacterStatsController;

    private void Start() {
        m_CharacterStatsController = character.GetComponent<CharacterStatsController>();
        DebugUtility.HandleErrorIfNullGetComponent<CharacterStatsController, HealthBarController>(m_CharacterStatsController, this, gameObject);

        SetHealthBarMaxValue(m_CharacterStatsController.maxHealth.GetValue());
        m_CharacterStatsController.onHealthChanged += HandleHealthChange;
    }
    public void SetHealthBarMaxValue(float health)
    {
        slider.maxValue = health;
        slider.value = slider.maxValue;
    }

    public void SetHealthBarFill(int health) {
        slider.value = health;
    }

    private void HandleHealthChange (float change) {
        SetHealthBarFill((int) m_CharacterStatsController.currentHealth);
    }
}
