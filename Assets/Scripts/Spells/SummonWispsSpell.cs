using System.Collections.Generic;
using UnityEngine;
using System;
using Random=UnityEngine.Random;
public enum WispState {
    Spawned,
    Dance,
    Ascent,
    Chase,
    StarFallEntrance,
    StarFall
};


[RequireComponent(typeof(ProjectileBase))]
public class SummonWispsSpell : MonoBehaviour
{
    [Header("General")]
    [Tooltip("Fall radius of how close to get to target before starFall")]
    public float fallRadius = 3f;
    [Tooltip("Radius of this Wisp's collision detection")]
    public float radius = 0.5f;
    [Tooltip("Transform representing the root of the Wisp (used for accurate collision detection)")]
    public Transform root;
    [Tooltip("Transform representing the tip of the Wisp (used for accurate collision detection)")]
    public Transform tip;

    [Tooltip("LifeTime of the Wisp")]
    public float maxLifeTime = 120f;
    [Tooltip("VFX prefab to spawn upon impact")]
    public GameObject impactVFX;
    [Tooltip("LifeTime of the VFX before being destroyed")]
    public float impactVFXLifetime = 5f;
    [Tooltip("Offset along the hit normal where the VFX will be spawned")]
    public float impactVFXSpawnOffset = 0.1f;
    [Tooltip("Clip to play on impact")]
    public AudioClip impactSFXClip;
    [Tooltip("Layers this Wisp can collide with")]
    public LayerMask hittableLayers = -1;

    [Header("Movement")]
    [Tooltip("Speed of the Wisp")]
    public float speed = 2f;
    [Tooltip("Max speed of the Wisp")]
    public float maxSpeed = 5f;
    [Tooltip("Search speed of the Wisp")]
    public float searchSpeed = 2f;
    [Tooltip("Starfall speed of the Wisp")]
    public float starFallSpeed = 50f;

    [Header("Damage")]
    [Tooltip("Damage of the projectile")]
    public float damage = 40f;
    [Tooltip("Slow duration")]
    public float slowDuration = 3f;
    [Tooltip("Slow amount")]
    public float slowAmount = 0.5f;


    [Header("Debug")]
    [Tooltip("Color of the Wisp radius debug view")]
    public Color radiusColor = Color.cyan * 0.2f;

    bool m_IsPlayerSpell = false;
    ProjectileBase m_ProjectileBase;
    Vector3 m_LastRootPosition;
    Vector3 m_Velocity;
    float m_SpawnTime;
    List<Collider> m_IgnoredColliders;

    WispState m_State = WispState.Spawned;
    WispStrategyParams m_Params;
    float m_LastJitter = 0;
    Vector3 m_Direction;
    float m_CurrentSpeed;

    Transform targetCharacter;
    Vector3 m_FallTarget;
    float m_RotationSpeed = 0;

    float m_Momentum;
    float m_Gravity;
    float m_Forward;
    float m_Up;

    float m_ChaseStart=0;
    private void OnEnable()
    {
        m_ProjectileBase = GetComponent<ProjectileBase>();
        DebugUtility.HandleErrorIfNullGetComponent<ProjectileBase, SummonWispsSpell>(m_ProjectileBase, this, gameObject);
        m_Params = GetComponent<WispStrategyParams>();
        DebugUtility.HandleErrorIfNullGetComponent<WispStrategyParams, SummonWispsSpell>(m_Params, this, gameObject);

        m_ProjectileBase.onShoot += OnShoot;

        Destroy(gameObject, maxLifeTime);
    }

    void OnShoot()
    {
        m_SpawnTime = Time.time;
        m_LastRootPosition = root.position + Random.insideUnitSphere*2;
        m_Velocity = (0.5f+Random.value)*speed*(Vector3.up + Random.insideUnitSphere/2f);
        m_Direction = m_Velocity.normalized;
        m_CurrentSpeed = Vector3.Magnitude(m_Velocity);
        m_IgnoredColliders = new List<Collider>();
        SetTeam();
        targetCharacter = GetTarget();
        SetDanceParams();
        // Ignore colliders of owner
        Collider[] ownerColliders = m_ProjectileBase.owner.GetComponentsInChildren<Collider>();
        m_IgnoredColliders.AddRange(ownerColliders);
        Collider[] selfColliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in selfColliders) {
            print(col.name);
        }
        m_IgnoredColliders.AddRange(selfColliders);
    }

    void Update()
    {
        Vector3 pos = transform.position;
        if (m_State == WispState.Spawned) {
            if (transform.position.y > 3f) {
                m_State = WispState.Dance;
            }
            m_Velocity = 0.99f*m_Velocity + 0.01f*Vector3.up*speed;
            m_Direction = m_Velocity.normalized;
            m_CurrentSpeed = m_Velocity.magnitude;

        } else if (m_State == WispState.Dance) {
            if (Time.time - m_SpawnTime > m_Params.timeToGo) {
                m_State = WispState.Ascent;
            }
            Vector3 ownerPos = m_ProjectileBase.owner.transform.position;
            Vector3 centerDir = Vector3.Normalize(ownerPos - pos);
            Vector3 rightDir = Vector3.Cross(centerDir, Vector3.up);
            if (Vector3.Distance(pos, ownerPos) > 2) {
                m_Direction = m_Momentum*m_Direction + m_Gravity*centerDir + m_Forward*rightDir + m_Up*Vector3.up;
                m_Direction = m_Direction.normalized;
                if (m_CurrentSpeed < maxSpeed) {
                    m_CurrentSpeed += 0.005f;
                }
            }
            m_Velocity = m_Direction * m_CurrentSpeed;

        } else if (m_State == WispState.Ascent) {
            if (pos.y > m_Params.highEnough) {
                m_ChaseStart = Time.time;
                m_State = WispState.Chase;
            }
            m_Direction = Vector3.Normalize(0.99f*m_Direction + 0.01f*Vector3.up*speed);
            if (m_CurrentSpeed < maxSpeed) {
                m_CurrentSpeed += 0.005f;
            }
            m_Velocity = m_Direction * m_CurrentSpeed;
            
        } else if (m_State == WispState.Chase) {
            Tuple<Vector3, bool> result = somethingUnder();
            if (result.Item2 && Time.time-m_ChaseStart > m_Params.minTimeBeforeFall) {
                m_State = WispState.StarFallEntrance;
                m_FallTarget = result.Item1;
            }
            if (m_CurrentSpeed > searchSpeed) {
                m_CurrentSpeed -= 0.01f;
            }
            Vector3 targetDirection = Vector3.Normalize(targetCharacter.position - pos);
            m_Direction = 0.9f*m_Direction + 0.1f*targetDirection;
            m_Direction.y = 0f;
            m_Direction = m_Direction.normalized;
            m_Velocity = m_Direction*m_CurrentSpeed;
            
        } else if (m_State == WispState.StarFallEntrance) {
            m_State = WispState.StarFall;
            m_Direction = Vector3.Normalize(m_FallTarget - pos);
            m_Velocity = m_CurrentSpeed * m_Direction;
            
        } else if (m_State == WispState.StarFall) {
            if (m_CurrentSpeed < starFallSpeed) {
                m_CurrentSpeed += 1f;
                m_Velocity = m_CurrentSpeed * m_Direction;
            }
            CheckHit();
        }

        // JitterRotation();
        JitterDirection();
        transform.position += m_Velocity * Time.deltaTime;
        // transform.RotateAround(pos, transform.up, Time.deltaTime *m_RotationSpeed* pos.y);

    }

    private void SetDanceParams() {
        m_Momentum = 0.9f + 0.1f*(Random.value*2f-1f);
        m_Gravity = (1-m_Momentum) * (0.2f + 0.1f*(Random.value*2f-1f));
        m_Forward = (1-m_Momentum-m_Gravity) * 0.9f;
        m_Up = (1-m_Momentum-m_Gravity) * 0.1f;
    }

    private void CheckHit() {
        // Hit detection
        {
            RaycastHit closestHit = new RaycastHit();
            closestHit.distance = Mathf.Infinity;
            bool foundHit = false;

            // Sphere cast
            RaycastHit[] hits = Physics.SphereCastAll(transform.position, radius, m_Direction, m_CurrentSpeed*Time.deltaTime, hittableLayers);
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
    }

    private bool IsHitValid(Collider collider)
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

    private void OnHit(Collider collider)
    { 
        // point damage
        InteractionDamageSelf damageable = collider.GetComponent<InteractionDamageSelf>();
        if (damageable)
        {
            damageable.iteractiableBase.Interact(gameObject);
            CharacterStatsController stats = collider.GetComponent<CharacterStatsController>();
            if (stats != null) {
                stats.moveSpeed.AddTemporaryModifier(slowAmount, slowDuration);
            }
        }
        print(string.Format("hit : {0}", collider.name));
        Destroy(this.gameObject);
    }

    private Transform GetTarget() {
        if (m_IsPlayerSpell) {
            return EnemyManager.instance.enemy.transform;
        } else {
            return PlayerManager.instance.player.transform;
        }
    }
    private void SetTeam() {
        m_IsPlayerSpell = (m_ProjectileBase.owner.GetComponent<PlayerStatsController>() != null);

        if (m_IsPlayerSpell) {
            ObjectLayers.instance.SetLayerRecursively(gameObject, ObjectLayers.instance.player);
        } else {
            ObjectLayers.instance.SetLayerRecursively(gameObject, ObjectLayers.instance.enemy);
        }

    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = radiusColor;
        Gizmos.DrawSphere(transform.position, radius);
    }

    private void JitterDirection() {
        if (Time.time - m_LastJitter > m_Params.stepDuration) {
            m_Direction = Vector3.Normalize(m_Direction + Random.insideUnitSphere*speed/2f); 
            m_Velocity = m_Direction * m_CurrentSpeed;
            m_LastJitter = Time.time;
        }
    }

    private void JitterRotation() { // call before jitter direction;
        if (Time.time - m_LastJitter < m_Params.stepDuration) {
            if (Random.value < .5) {
                m_RotationSpeed = Random.value * 20f;
            } else {
                m_RotationSpeed = 0f;
            }
        }
    }

    private Tuple<Vector3, bool> somethingUnder() {
        Vector3 pos = transform.position;
        Vector3 targetPos = targetCharacter.position;
        Vector2 selfBirdPos = new Vector2(pos.x, pos.z);
        Vector2 targetBirdPos = new Vector2(targetPos.x, targetPos.z);
        if (Vector2.Distance(selfBirdPos, targetBirdPos) < fallRadius) {
            return new Tuple<Vector3, bool> (targetPos, true);
        }
        // RaycastHit hit;
        // if (Physics.SphereCast(transform.position, fallRadius, Vector3.down, out hit))
        // {
        //     if (IsHitValid(hit.collider)) {
        //         return Tuple<Vector3, bool>(hit.transform.position, true);
        //     }
        // }
        return new Tuple<Vector3, bool>(Vector3.zero, false);
    }

}