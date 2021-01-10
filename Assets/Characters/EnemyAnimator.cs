using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
public class EnemyAnimator : MonoBehaviour
{
    public const float animationTransitionTime = 0.1f;
    Animator m_animator;
    NavMeshAgent m_navMeshAgent;

    void Start()
    {
        m_animator = GetComponentInChildren<Animator>();
        m_navMeshAgent = GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        float speedPercent = m_navMeshAgent.velocity.magnitude / m_navMeshAgent.speed;
        m_animator.SetFloat("speedPercent", speedPercent, animationTransitionTime, Time.deltaTime);
    }
}
