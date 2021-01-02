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
public class EnemyController : MonoBehaviour
{
    public float lookRadius = 10f;
    public float stoppingRatio = 0.8f;
    public float delayBetweenAttacks = 1f;
    NavMeshAgent m_NavAgent;
    Transform m_PlayerTransform;
    InteractableBase m_FocusedPlayer;
    InteractableBase m_PlayerInteractableBase;

    float m_LastTimeShot = 0f;
    float m_cdr;
    float m_moveSpeed;
    public EnemyState m_EnemyState = EnemyState.Idle;
    CharacterStatsController m_CharacterStatsController;
    void Start ()
    {
        m_NavAgent = GetComponent<NavMeshAgent>();
        m_PlayerTransform = PlayerManager.instance.player.transform;
        m_PlayerInteractableBase = PlayerManager.instance.player.GetComponent<InteractableBase>();
        m_NavAgent.stoppingDistance = 0;

        m_CharacterStatsController = GetComponent<CharacterStatsController>();
        m_cdr = m_CharacterStatsController.cdr.GetValue();
        m_moveSpeed = m_CharacterStatsController.moveSpeed.GetValue();
        m_NavAgent.speed = m_moveSpeed;
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
            float distanceToPlayer = Vector3.Distance(m_FocusedPlayer.transform.position, transform.position);
            if (distanceToPlayer <= stoppingRatio*m_FocusedPlayer.radius) {
                m_EnemyState = EnemyState.Fighting;
                StopMoving();
            } else if (distanceToPlayer <= lookRadius) {
                SetNavDestination(m_FocusedPlayer.transform.position);
            } else {
                SetNavDestination(m_FocusedPlayer.transform.position);
                // currently follows u to the end of days if ur hunted
                // m_EnemyState = EnemyState.Idle;
            }
        } else if (m_EnemyState == EnemyState.Fighting) {
            float distanceToPlayer = Vector3.Distance(m_FocusedPlayer.transform.position, transform.position);
            if (distanceToPlayer <= stoppingRatio*m_FocusedPlayer.radius) {
                Fight();
            } else {
                m_EnemyState = EnemyState.Chasing;
            }
        }
    }

    private void Fight() {
        TryAttack();
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

    private void SetNavDestination(Vector3 position)
    {
        m_NavAgent.isStopped = false;
        m_NavAgent.SetDestination(position);
    }

    private void StopMoving()
    {
        m_NavAgent.isStopped = true;
        m_NavAgent.ResetPath();
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
