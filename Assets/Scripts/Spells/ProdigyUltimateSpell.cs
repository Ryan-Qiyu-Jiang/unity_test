using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ProjectileBase))]
public class ProdigyUltimateSpell : MonoBehaviour
{
    [Tooltip("Duration of buff")]
    public float duration = 20f;

    [Tooltip("Speed modifier")]
    public float speed = 1.2f;

    [Tooltip("CDR modifier")]
    public float cdr = 0.5f;

    [Tooltip("Initial Speed Boost")]
    public float speedBoost = 1.2f;

    [Tooltip("Initial Speed Boost Duration")]
    public float speedBoostDuration = 1f;
    
    ProjectileBase m_ProjectileBase;
    private void OnEnable()
    {
        m_ProjectileBase = GetComponent<ProjectileBase>();
        DebugUtility.HandleErrorIfNullGetComponent<ProjectileBase, ProdigyUltimateSpell>(m_ProjectileBase, this, gameObject);

        m_ProjectileBase.onShoot += OnShoot;
        Destroy(gameObject, 1);
    }

    void OnShoot () {
        PlayerStatsController playerStatsController = m_ProjectileBase.owner.GetComponent<PlayerStatsController>();
        DebugUtility.HandleErrorIfNullGetComponent<PlayerStatsController, ProdigyUltimateSpell>(playerStatsController, this, gameObject);

        playerStatsController.cdr.AddTemporaryModifier(cdr, duration);
        playerStatsController.moveSpeed.AddTemporaryModifier(speed, duration);
        playerStatsController.moveSpeed.AddTemporaryModifier(speedBoost, speedBoostDuration);
    }
}
