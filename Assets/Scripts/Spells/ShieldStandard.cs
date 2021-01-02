using System.Collections.Generic;
using UnityEngine;
using System;

[RequireComponent(typeof(ProjectileBase))]
public class ShieldStandard : MonoBehaviour
{
    [Header("General")]
    [Tooltip("Radius of this Shield's collision detection")]
    public float radius = 5f;
    [Tooltip("Transform representing the root of the Shield (used for accurate collision detection)")]
    public Transform root;

    [Tooltip("LifeTime of the Shield")]
    public float maxLifeTime = 5f;
    [Tooltip("VFX prefab to spawn upon impact")]
    public GameObject impactVFX;
    [Tooltip("LifeTime of the VFX before being destroyed")]
    public float impactVFXLifetime = 5f;
    [Tooltip("Offset along the hit normal where the VFX will be spawned")]
    public float impactVFXSpawnOffset = 0.1f;
    [Tooltip("Clip to play on impact")]
    public AudioClip impactSFXClip;
    [Tooltip("Layers this Shield can collide with")]
    public LayerMask hittableLayers = -1;

    [Header("Movement")]
    [Tooltip("Speed of the Shield")]
    public float speed = 0f;
    [Tooltip("Downward acceleration from gravity")]
    public float gravityDownAcceleration = 0f;

    [Header("Debug")]
    [Tooltip("Color of the Shield radius debug view")]
    public Color radiusColor = Color.cyan * 0.2f;

    ProjectileBase m_ProjectileBase;
    Vector3 m_LastRootPosition;
    Vector3 m_Velocity;
    bool m_HasTrajectoryOverride;
    float m_ShootTime;
    Vector3 m_TrajectoryCorrectionVector;
    Vector3 m_ConsumedTrajectoryCorrectionVector;
    List<Collider> m_IgnoredColliders;

    const QueryTriggerInteraction k_TriggerInteraction = QueryTriggerInteraction.Collide;

    private void OnEnable()
    {
        m_ProjectileBase = GetComponent<ProjectileBase>();
        DebugUtility.HandleErrorIfNullGetComponent<ProjectileBase, ShieldStandard>(m_ProjectileBase, this, gameObject);

        m_ProjectileBase.onShoot += OnShoot;

        Destroy(gameObject, maxLifeTime);
    }

    void OnShoot()
    {
        m_ShootTime = Time.time;
        m_LastRootPosition = root.position;
        m_Velocity = Vector3.zero;
        // m_IgnoredColliders = new List<Collider>();

        // // Ignore colliders of owner
        // Collider[] ownerColliders = m_ProjectileBase.owner.GetComponentsInChildren<Collider>();
        // m_IgnoredColliders.AddRange(ownerColliders);
    }

    void Update()
    {
        // Move
        // transform.position = m_LastRootPosition;

        // Hit detection
        // {
        //     RaycastHit closestHit = new RaycastHit();
        //     closestHit.distance = Mathf.Infinity;
        //     bool foundHit = false;

            // Sphere cast
            // Vector3 displacementSinceLastFrame = tip.position - m_LastRootPosition;
            // RaycastHit[] hits = Physics.SphereCastAll(m_LastRootPosition, radius, displacementSinceLastFrame.normalized, displacementSinceLastFrame.magnitude, hittableLayers, k_TriggerInteraction);
            // foreach (var hit in hits)
            // {
            //     if (IsHitValid(hit) && hit.distance < closestHit.distance)
            //     {
            //         foundHit = true;
            //         closestHit = hit;
            //     }
            // }

            // if (foundHit)
            // {
            //     // Handle case of casting while already inside a collider
            //     if(closestHit.distance <= 0f)
            //     {
            //         closestHit.point = root.position;
            //         closestHit.normal = -transform.forward;
            //     }

            //     OnHit(closestHit.point, closestHit.normal, closestHit.collider);
            // }
        // }

        // m_LastRootPosition = root.position;
    }

    // bool IsHitValid(RaycastHit hit)
    // {
    //     // ignore hits with an ignore component
    //     if(hit.collider.GetComponent<IgnoreHitDetection>())
    //     {
    //         return false;
    //     }

    //     // ignore hits with triggers that don't have a Damageable component
    //     if(hit.collider.isTrigger && hit.collider.GetComponent<Damageable>() == null)
    //     {
    //         return false;
    //     }

    //     // ignore hits with specific ignored colliders (self colliders, by default)
    //     if (m_IgnoredColliders != null && m_IgnoredColliders.Contains(hit.collider))
    //     {
    //         return false;
    //     }

    //     return true;
    // }

    // void OnHit(Vector3 point, Vector3 normal, Collider collider)
    // { 
    //     // damage
    //     if (areaOfDamage)
    //     {
    //         // area damage
    //         areaOfDamage.InflictDamageInArea(damage, point, hittableLayers, k_TriggerInteraction, m_ProjectileBase.owner);
    //     }
    //     else
    //     {
    //         // point damage
    //         Damageable damageable = collider.GetComponent<Damageable>();
    //         if (damageable)
    //         {
    //             damageable.InflictDamage(damage, false, m_ProjectileBase.owner);
    //         }
    //     }

    //     // impact vfx
    //     if (impactVFX)
    //     {
    //         GameObject impactVFXInstance = Instantiate(impactVFX, point + (normal * impactVFXSpawnOffset), Quaternion.LookRotation(normal));
    //         if (impactVFXLifetime > 0)
    //         {
    //             Destroy(impactVFXInstance.gameObject, impactVFXLifetime);
    //         }
    //     }

    //     // impact sfx
    //     if (impactSFXClip)
    //     {
    //         AudioUtility.CreateSFX(impactSFXClip, point, AudioUtility.AudioGroups.Impact, 1f, 3f);
    //     }

    //     // Self Destruct
    //     Destroy(this.gameObject);
    // }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = radiusColor;
        Gizmos.DrawSphere(transform.position, radius);
    }
}