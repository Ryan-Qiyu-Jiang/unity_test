using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ProjectileBase))]
public class SpeedBoostSpell : MonoBehaviour
{
    [Tooltip("Duration of buff")]
    public float duration = 2f;

    [Tooltip("Speed modifier")]
    public float speed = 1.2f;
    
    ProjectileBase m_ProjectileBase;
    private void OnEnable()
    {
        m_ProjectileBase = GetComponent<ProjectileBase>();
        DebugUtility.HandleErrorIfNullGetComponent<ProjectileBase, SpeedBoostSpell>(m_ProjectileBase, this, gameObject);

        m_ProjectileBase.onShoot += OnShoot;
        Destroy(gameObject, 1);
    }

    void OnShoot () {
        CharacterStatsController statsController = m_ProjectileBase.owner.GetComponent<CharacterStatsController>();
        DebugUtility.HandleErrorIfNullGetComponent<CharacterStatsController, SpeedBoostSpell>(statsController, this, gameObject);

        statsController.moveSpeed.AddTemporaryModifier(speed, duration);
    }
}
