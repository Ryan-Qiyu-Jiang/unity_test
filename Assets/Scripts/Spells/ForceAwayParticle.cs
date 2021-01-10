using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ProjectileBase))]
public class ForceAwayParticle : MonoBehaviour
{
    [Header("General")]
    [Tooltip("Radius of this Shield to set initial position")]
    public float radius = 20f;
    [Tooltip("Transform representing the root of the Shield (used for accurate collision detection)")]
    public Transform root;

    [Tooltip("LifeTime of the Shield")]
    public float maxLifeTime = 1f;

    [Tooltip("Acceleration of those pushed away")]
    public float acceleration = 1f;
    ProjectileBase m_ProjectileBase;

    bool m_IsPlayerSpell = true;
    LayerMask m_HittableLayers; 

    private void OnEnable()
    {
        m_ProjectileBase = GetComponent<ProjectileBase>();
        DebugUtility.HandleErrorIfNullGetComponent<ProjectileBase, ShieldStandard>(m_ProjectileBase, this, gameObject);

        m_ProjectileBase.onShoot += OnShoot;

        Destroy(gameObject, maxLifeTime);
    }

    void OnShoot()
    {
        m_IsPlayerSpell = (m_ProjectileBase.owner.GetComponent<PlayerStatsController>() != null);
        if (m_IsPlayerSpell) {
            m_HittableLayers = LayerMask.GetMask("Enemy");
        } else {
            m_HittableLayers = LayerMask.GetMask("Player");
        }
    }

    void Update()
    {
        // Sphere cast
        Collider[] colliders = Physics.OverlapSphere(root.position, radius, m_HittableLayers);
        foreach (Collider hit in colliders)
        {
            if (IsHitValid(hit))
            {
                print(string.Format("force hit {0}", hit.name));
                Vector3 outwardsDirection = Vector3.Normalize(hit.transform.position - root.position);
                float closeness = 1-Vector3.Magnitude(hit.transform.position - root.position)/radius;
                hit.transform.position += outwardsDirection * acceleration * closeness;
            } else {
                print(string.Format("force invalid hit {0}", hit.name));
            }
        }
    }

    bool IsHitValid(Collider collider) {
        return true;
    }
}
