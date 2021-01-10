﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ProjectileBase))]
public class PierceSpell : MonoBehaviour
{
    [Header("General")]
    [Tooltip("Radius of this projectile's collision detection")]
    public float radius = 0.01f;
    [Tooltip("Transform representing the root of the projectile (used for accurate collision detection)")]
    public Transform root;
    [Tooltip("Transform representing the tip of the projectile (used for accurate collision detection)")]
    public Transform tip;
    [Tooltip("LifeTime of the projectile")]
    public float maxLifeTime = 5f;
    [Tooltip("VFX prefab to spawn upon impact")]
    public GameObject impactVFX;
    [Tooltip("LifeTime of the VFX before being destroyed")]
    public float impactVFXLifetime = 5f;
    [Tooltip("Offset along the hit normal where the VFX will be spawned")]
    public float impactVFXSpawnOffset = 0.1f;
    [Tooltip("Clip to play on impact")]
    public AudioClip impactSFXClip;
    [Tooltip("Layers this projectile can collide with")]
    public LayerMask hittableLayers = -1;
    [Header("Movement")]
    [Tooltip("Speed of the projectile")]
    public float speed = 30f;
    [Tooltip("Downward acceleration from gravity")]
    public float gravityDownAcceleration = 0f;
    [Tooltip("Distance over which the projectile will correct its course to fit the intended trajectory (used to drift projectiles towards center of screen in First Person view). At values under 0, there is no correction")]
    public float trajectoryCorrectionDistance = -1;
    [Tooltip("Determines if the projectile inherits the velocity that the Spell's muzzle had when firing")]
    public bool inheritSpellVelocity = false;
    [Header("Damage")]
    [Tooltip("Damage of the projectile")]
    public float damage = 40f;
    [Tooltip("Slow duration")]
    public float slowDuration = 3f;
    [Tooltip("Slow amount")]
    public float slowAmount = 0.5f;

    [Header("Debug")]
    [Tooltip("Color of the projectile radius debug view")]
    public Color radiusColor = Color.cyan * 0.2f;

    ProjectileBase m_ProjectileBase;
    Vector3 m_LastRootPosition;
    Vector3 m_Velocity;
    bool m_HasTrajectoryOverride;
    float m_ShootTime;
    Vector3 m_TrajectoryCorrectionVector;
    Vector3 m_ConsumedTrajectoryCorrectionVector;
    List<Collider> m_IgnoredColliders;

    bool m_IsPlayerSpell = false;
    const QueryTriggerInteraction k_TriggerInteraction = QueryTriggerInteraction.Collide;

    private void OnEnable()
    {
        m_ProjectileBase = GetComponent<ProjectileBase>();
        DebugUtility.HandleErrorIfNullGetComponent<ProjectileBase, ProjectileStandard>(m_ProjectileBase, this, gameObject);

        m_ProjectileBase.onShoot += OnShoot;

        Destroy(gameObject, maxLifeTime);
    }

    void OnShoot()
    {
        m_ShootTime = Time.time;
        m_LastRootPosition = root.position;
        m_Velocity = transform.forward * speed;
        m_IgnoredColliders = new List<Collider>();
        transform.position += m_ProjectileBase.inheritedMuzzleVelocity * Time.deltaTime;

        // Ignore colliders of owner
        Collider[] ownerColliders = m_ProjectileBase.owner.GetComponentsInChildren<Collider>();
        m_IgnoredColliders.AddRange(ownerColliders);

        // Handle case of player shooting (make projectiles not go through walls, and remember center-of-screen trajectory)
        Transform spellCameraPosition;
        Transform SpellParentSocket;
        m_IsPlayerSpell = (m_ProjectileBase.owner.GetComponent<PlayerStatsController>() != null);

        if (m_IsPlayerSpell) {
            PlayerSpellsManager playerSpellsManager = m_ProjectileBase.owner.GetComponent<PlayerSpellsManager>();
            SpellParentSocket = playerSpellsManager.SpellParentSocket.transform;
            spellCameraPosition = playerSpellsManager.spellCamera.transform;
        } else {
            EnemySpellsManager enemySpellsManager = m_ProjectileBase.owner.GetComponent<EnemySpellsManager>();
            SpellParentSocket = enemySpellsManager.SpellParentSocket.transform;
            spellCameraPosition = enemySpellsManager.spellCamera.transform;
        }

        m_HasTrajectoryOverride = true;
        Vector3 cameraToMuzzle = m_ProjectileBase.initialPosition;

        m_TrajectoryCorrectionVector = Vector3.ProjectOnPlane(-cameraToMuzzle, SpellParentSocket.forward);
        if (trajectoryCorrectionDistance == 0)
        {
            transform.position += m_TrajectoryCorrectionVector;
            m_ConsumedTrajectoryCorrectionVector = m_TrajectoryCorrectionVector;
        }
        else if (trajectoryCorrectionDistance < 0)
        {
            m_HasTrajectoryOverride = false;
        }

        if (Physics.Raycast(spellCameraPosition.position, cameraToMuzzle.normalized, out RaycastHit hit, cameraToMuzzle.magnitude, hittableLayers, k_TriggerInteraction))
        {
            if (IsHitValid(hit.collider))
            {
                OnHit(hit.collider);
            }
        }
    }

    void Update()
    {
        // Move
        transform.position += m_Velocity * Time.deltaTime;
        if (inheritSpellVelocity)
        {
            transform.position += m_ProjectileBase.inheritedMuzzleVelocity * Time.deltaTime;
        }

        // Drift towards trajectory override (this is so that projectiles can be centered 
        // with the camera center even though the actual Spell is offset)
        if (m_HasTrajectoryOverride && m_ConsumedTrajectoryCorrectionVector.sqrMagnitude < m_TrajectoryCorrectionVector.sqrMagnitude)
        {
            Vector3 correctionLeft = m_TrajectoryCorrectionVector - m_ConsumedTrajectoryCorrectionVector;
            float distanceThisFrame = (root.position - m_LastRootPosition).magnitude;
            Vector3 correctionThisFrame = (distanceThisFrame / trajectoryCorrectionDistance) * m_TrajectoryCorrectionVector;
            correctionThisFrame = Vector3.ClampMagnitude(correctionThisFrame, correctionLeft.magnitude);
            m_ConsumedTrajectoryCorrectionVector += correctionThisFrame;

            // Detect end of correction
            if(m_ConsumedTrajectoryCorrectionVector.sqrMagnitude == m_TrajectoryCorrectionVector.sqrMagnitude)
            {
                m_HasTrajectoryOverride = false;
            }

            transform.position += correctionThisFrame;
        }

        // Orient towards velocity
        transform.forward = m_Velocity.normalized;

        // Gravity
        if (gravityDownAcceleration > 0)
        {
            // add gravity to the projectile velocity for ballistic effect
            m_Velocity += Vector3.down * gravityDownAcceleration * Time.deltaTime;
        }

        // Hit detection
        {
            RaycastHit closestHit = new RaycastHit();
            closestHit.distance = Mathf.Infinity;
            bool foundHit = false;

            // Sphere cast
            Vector3 displacementSinceLastFrame = tip.position - m_LastRootPosition;
            RaycastHit[] hits = Physics.SphereCastAll(m_LastRootPosition, radius, displacementSinceLastFrame.normalized, displacementSinceLastFrame.magnitude, hittableLayers, k_TriggerInteraction);
            foreach (var hit in hits)
            {
                if (IsHitValid(hit.collider) && hit.distance < closestHit.distance)
                {
                    foundHit = true;
                    closestHit = hit;
                }
            }

            if (foundHit)
            {
                // Handle case of casting while already inside a collider
                if(closestHit.distance <= 0f)
                {
                    closestHit.point = root.position;
                    closestHit.normal = -transform.forward;
                }

                OnHit(closestHit.collider);
            }
        }

        m_LastRootPosition = root.position;
    }

    bool IsHitValid(Collider collider)
    {   
        // ignore hits with an ignore component
        if(collider.GetComponent<IgnoreHitDetection>())
        {
            return false;
        }

        // ignore hits with specific ignored colliders (self colliders, by default)
        if (m_IgnoredColliders != null && m_IgnoredColliders.Contains(collider))
        {
            return false;
        }

        return true;
    }
    void OnHit(Collider collider)
    { 
        // point damage
        InteractionDamageSelf damageable = collider.GetComponent<InteractionDamageSelf>();
        if (damageable)
        {
            damageable.iteractiableBase.Interact(gameObject);
            CharacterStatsController stats = collider.GetComponent<CharacterStatsController>();
            print("HIT!");
            if (stats != null) {
                print("Slowed!");
                stats.moveSpeed.AddTemporaryModifier(slowAmount, slowDuration);
            }
        }

        // // impact vfx
        // if (impactVFX)
        // {
        //     GameObject impactVFXInstance = Instantiate(impactVFX, point + (normal * impactVFXSpawnOffset), Quaternion.LookRotation(normal));
        //     if (impactVFXLifetime > 0)
        //     {
        //         Destroy(impactVFXInstance.gameObject, impactVFXLifetime);
        //     }
        // }

        // // impact sfx
        // if (impactSFXClip)
        // {
        //     AudioUtility.CreateSFX(impactSFXClip, point, AudioUtility.AudioGroups.Impact, 1f, 3f);
        // }

        Destroy(this.gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = radiusColor;
        Gizmos.DrawSphere(transform.position, radius);
    }
}