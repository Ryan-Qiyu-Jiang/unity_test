using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyState
{
    Idle,
    Chasing,
    Fighting,
}

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyCharacterController : MonoBehaviour
{
    public float lookRadius = 10f;
    public float stoppingRatio = 0.8f;
    public float delayBetweenAttacks = 1f;
    public Camera spellCamera;
    NavMeshAgent m_NavAgent;
    Transform m_PlayerTransform;
    InteractableBase m_FocusedPlayer;
    InteractableBase m_PlayerInteractableBase;
    InteractableBase m_SelfInteractableBase;

    EnemyStrategy m_strategy = new HangedManStrategy();

    float m_LastTimeShot = 0f;
    float m_cdr;
    float m_moveSpeed;
    public EnemyState m_EnemyState = EnemyState.Idle;
    CharacterStatsController m_CharacterStatsController;
    void Start ()
    {
        m_NavAgent = GetComponent<NavMeshAgent>();
        DebugUtility.HandleErrorIfNullGetComponent<NavMeshAgent, EnemyCharacterController>(m_NavAgent, this, gameObject);

        m_PlayerTransform = PlayerManager.instance.player.transform;
        m_PlayerInteractableBase = PlayerManager.instance.player.GetComponent<InteractableBase>();
        m_SelfInteractableBase = GetComponent<InteractableBase>();
        m_NavAgent.stoppingDistance = 0;

        m_CharacterStatsController = GetComponent<CharacterStatsController>();
        m_cdr = m_CharacterStatsController.cdr.GetValue();
        m_moveSpeed = m_CharacterStatsController.moveSpeed.GetValue();
        m_NavAgent.speed = m_moveSpeed;

        m_strategy.selfGameObject = gameObject;
        m_strategy.playerGameObject = PlayerManager.instance.player;
        m_strategy.Init();
        ChaseOnBeingAttacked();
    }

    void Update ()
    {
        if (m_EnemyState == EnemyState.Idle) {
            float distanceToPlayer = Vector3.Distance(m_PlayerInteractableBase.transform.position, transform.position);
            if (distanceToPlayer <= lookRadius) {
                m_FocusedPlayer = m_PlayerInteractableBase;
                m_EnemyState = EnemyState.Chasing;
            }
        } else if (m_EnemyState == EnemyState.Chasing) {
            Fight();
        }
    }

    void ChaseOnBeingAttacked() {
        m_SelfInteractableBase.onInteract += FocusAttacker;
    }

    void FocusAttacker(GameObject caller) {
        if (m_EnemyState == EnemyState.Idle) {
            ProjectileBase spell = caller.GetComponent<ProjectileBase>();
            if (spell != null) {
                m_EnemyState = EnemyState.Chasing;
                m_FocusedPlayer = spell.owner.GetComponent<InteractableBase>();
            } else if (caller.GetComponent<PlayerStatsController>() != null) {
                m_EnemyState = EnemyState.Chasing;
                m_FocusedPlayer = caller.GetComponent<InteractableBase>();
            }
        }
    }

    private void Fight() {
        m_strategy.Move();
        // TryAttack();
    }

    private bool TryAttack()
    {
        if (m_LastTimeShot + delayBetweenAttacks*m_cdr < Time.time) {
            m_FocusedPlayer.Interact(gameObject);
            m_LastTimeShot = Time.time;
            return true;
        }
        return false;
    }

    public void SetNavDestination(Vector3 position)
    {
        m_NavAgent.isStopped = false;
        m_NavAgent.SetDestination(position);
    }

    public void UpdateSpeed()
    {
        m_moveSpeed = m_CharacterStatsController.moveSpeed.GetValue();
        m_NavAgent.speed = m_moveSpeed;
    }

    public void StopMoving()
    {
        m_NavAgent.isStopped = true;
        // m_NavAgent.ResetPath();
    }
    
    public void RemoveFocus()
    {
        if (m_FocusedPlayer != null) {
            m_FocusedPlayer = null;
            if (m_NavAgent.enabled) {
                m_NavAgent.ResetPath();
                m_NavAgent.updatePosition = false;
                m_NavAgent.updateRotation = false;
                m_NavAgent.isStopped = true;
                m_NavAgent.enabled = false;
            }
        }
    }

    void OnDrawGizmosSelected ()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, lookRadius);
    }
}
