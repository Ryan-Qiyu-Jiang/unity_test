using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ProjectileBase))]
public class PanicSpell : MonoBehaviour
{ 
    [Tooltip("Speed modifier")]
    public float speed = 1.2f;

    [Tooltip("CDR modifier")]
    public float cdr = 0.5f;
    [Tooltip("AP modifier")]
    public float ap = 1.5f;
    
    ProjectileBase m_ProjectileBase;
    private void OnEnable()
    {
        m_ProjectileBase = GetComponent<ProjectileBase>();
        DebugUtility.HandleErrorIfNullGetComponent<ProjectileBase, PanicSpell>(m_ProjectileBase, this, gameObject);

        m_ProjectileBase.onShoot += OnShoot;
        Destroy(gameObject, 1);
    }

    void OnShoot () {
        CharacterStatsController statsController = m_ProjectileBase.owner.GetComponent<CharacterStatsController>();
        DebugUtility.HandleErrorIfNullGetComponent<CharacterStatsController, PanicSpell>(statsController, this, gameObject);

        statsController.cdr.AddModifier(cdr);
        statsController.moveSpeed.AddModifier(speed);
        statsController.ap.AddModifier(ap);
        statsController.AddStatus(Status.LowPrecision);
    }
}
