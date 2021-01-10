using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ProjectileBase))]
public class UnholySmiteSpell : MonoBehaviour
{
    [Header("General")]
    [Tooltip("Radius of this projectile's collision detection")]
    public float radius = 2f;
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
    public float speed = 20f;
    [Tooltip("Rotation speed of the projectile")]
    public float rotationSpeed = 60f;
    [Tooltip("Determines if the projectile inherits the velocity that the Spell's muzzle had when firing")]
    public bool inheritSpellVelocity = false;
    [Header("Damage")]
    [Tooltip("Damage of the projectile")]
    public float damage = 50f;
    [Tooltip("Starting Depth")]
    public float startingDepth = 10f;

    ProjectileBase m_ProjectileBase;
    Vector3 m_LastRootPosition;
    Vector3 m_Velocity;
    float m_ShootTime;
    List<Collider> m_IgnoredColliders;

    Vector3 m_InitialPosition;
    float m_ObjectHeight;
    bool m_IsPlayerSpell = false;
    
    private void OnEnable()
    {
        m_ProjectileBase = GetComponent<ProjectileBase>();
        DebugUtility.HandleErrorIfNullGetComponent<ProjectileBase, ProjectileStandard>(m_ProjectileBase, this, gameObject);

        m_ProjectileBase.onShoot += OnShoot;

        Destroy(gameObject, maxLifeTime);
    }

    void OnShoot()
    {
        Collider[] ownerColliders = m_ProjectileBase.owner.GetComponentsInChildren<Collider>();
        m_IgnoredColliders = new List<Collider>();
        m_IgnoredColliders.AddRange(ownerColliders);

        m_ObjectHeight = tip.position.y - root.position.y;
        m_InitialPosition = root.position + Vector3.down*(m_ObjectHeight + startingDepth);
        m_Velocity = Vector3.up*speed;
        Transform spellCameraPosition;
        Transform SpellParentSocket;
        m_IsPlayerSpell = (m_ProjectileBase.owner.GetComponent<PlayerStatsController>() != null);

        if (m_IsPlayerSpell) {
            ObjectLayers.instance.SetLayerRecursively(gameObject, ObjectLayers.instance.playerProjectile);
            PlayerSpellsManager playerSpellsManager = m_ProjectileBase.owner.GetComponent<PlayerSpellsManager>();
            SpellParentSocket = playerSpellsManager.SpellParentSocket.transform;
            spellCameraPosition = playerSpellsManager.spellCamera.transform;
        } else {
            ObjectLayers.instance.SetLayerRecursively(gameObject, ObjectLayers.instance.enemyProjectile);
            EnemySpellsManager enemySpellsManager = m_ProjectileBase.owner.GetComponent<EnemySpellsManager>();
            SpellParentSocket = enemySpellsManager.SpellParentSocket.transform;
            spellCameraPosition = enemySpellsManager.spellCamera.transform;
        }

        if (Physics.Raycast(spellCameraPosition.position, m_ProjectileBase.initialDirection.normalized, out RaycastHit hit, 100f, hittableLayers))
        {
            if (IsHitValid(hit.collider))
            {
                m_InitialPosition = hit.point + Vector3.down*(m_ObjectHeight + startingDepth);
            }
        }
        transform.position = m_InitialPosition;
        transform.rotation = Quaternion.Euler(0, 30, 0);
    }

    void Update()
    {
        // Move
        if (transform.position.y < 0) {
            transform.position += m_Velocity * Time.deltaTime;
        }
        transform.RotateAround(transform.position, transform.up, Time.deltaTime *rotationSpeed* transform.position.y);

        // Hit detection
        {
            float distanceTravelled = transform.position.y-m_InitialPosition.y;
            RaycastHit[] hits = Physics.SphereCastAll(m_InitialPosition, radius, Vector3.up, m_ObjectHeight+distanceTravelled, hittableLayers);
            foreach (var hit in hits)
            {
                if (IsHitValid(hit.collider))
                {
                    OnHit(hit.collider);
                }
            }
        }
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
    }
}
