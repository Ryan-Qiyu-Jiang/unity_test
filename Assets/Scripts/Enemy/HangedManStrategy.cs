using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HangedManState
{
    MeleeEntrance,
    MeleeChase,
    StealthedChase,
    MeleeInRange,
    CasterEntrance,
    CasterChase,
    CasterKite,
    CasterPanicEntrance,
    CasterPanic,
    CasterSummonEntrance,
    CasterSummon,
    TheHangedMan
}

public class HangedManStrategy : EnemyStrategy
{
    public enum HangingState 
    {
        Begin,
        ModelChange,
        PlayAnimation,
        RemoveWorld,
        End
    }

    EnemySpellsManager m_SpellManager;
    // Basic Attack (Interact)
    // Stealth 0
    // Push 1
    // Impale 2
    // Smite 3
    // Summon 4
    // Panic 5
    // Charge 6
    // TheHangedManUltimate 7

    EnemyCharacterController m_SelfController;
    PlayerCharacterController m_PlayerController;
    CharacterStatsController m_SelfStats;
    PlayerStatsController m_PlayerStats;
    HangedManState m_State = HangedManState.MeleeChase;
    HangingState m_HangingState = HangingState.Begin;
    InteractableBase m_PlayerInteractable;
    StrategyParams m_Params;

    GameObject m_GFX;
    GameObject m_EnemyGraphic;
    GameObject m_TheHangingGraphic;
    GameObject m_MobileHealthBar;
    GameObject m_Level;
    EnemyAnimator m_Animator;

    float m_LastTakenDamagedTime = 0;
    float m_StealthStart = 0;
    float m_TimeStarted = 0;
    float m_DamageBalanced = 0;
    float m_DelayBetweenAttacks = 1f;
    float m_cdr = 1f;
    float m_LastTimeBasic = 0f;
    float m_SummoningStart = 0f;
    float m_HangingStartTime = 0;
    Vector3 m_PlayerPreviousPosition;
    float m_AnimationStartTime=0;
    float m_FallingStartTime=0;

    public override void Init() {
        m_SpellManager = selfGameObject.GetComponent<EnemySpellsManager>();
        m_SelfController = selfGameObject.GetComponent<EnemyCharacterController>();
        m_Params = selfGameObject.GetComponent<StrategyParams>();
        m_SelfStats = selfGameObject.GetComponent<CharacterStatsController>();
        m_PlayerController = playerGameObject.GetComponent<PlayerCharacterController>();
        m_PlayerStats = playerGameObject.GetComponent<PlayerStatsController>();
        m_SelfStats.onHealthChanged += UpdateDamageTime;
        m_DelayBetweenAttacks = m_SelfController.delayBetweenAttacks;
        m_cdr = m_SelfStats.cdr.GetValue();
        m_GFX = selfGameObject.transform.Find("GFX").gameObject;
        m_MobileHealthBar = m_GFX.transform.Find("MobileHealthBar").gameObject;
        m_EnemyGraphic = m_GFX.transform.Find("EnemyGraphic").gameObject;
        m_TheHangingGraphic = m_EnemyGraphic.transform.Find("Rope").gameObject;
        m_Level = LevelsManager.instance.level;
        m_Animator = selfGameObject.GetComponent<EnemyAnimator>();
        m_PlayerInteractable = playerGameObject.GetComponent<InteractableBase>();

        PlayerManager.instance.playerDamageTaken += (dmg) => {m_DamageBalanced += dmg;};
        EnemyManager.instance.enemyDamageTaken += (dmg) => {m_DamageBalanced -= dmg;};
    }
    
    public override void Move () {
        if (Time.time-m_TimeStarted > m_Params.timesUp) {
            m_State = HangedManState.TheHangedMan;
            // Debug.Log("the hanging has begun.");
            TheHanging();
            return;
        }

        float distance = GetDistance(selfGameObject, playerGameObject);
        if (m_State == HangedManState.MeleeChase) {
            Debug.Log("MeleeChase");
            if (distance <= m_Params.inRange) {
                m_State = HangedManState.MeleeInRange;
                m_SpellManager.CastSpell(6);
            } else if (distance > m_Params.minStealthRange) { // add cooldown Time.time-m_LastTakenDamagedTime < 30f
                m_State = HangedManState.StealthedChase;
                m_SpellManager.CastSpell(0);
                m_StealthStart = Time.time;
            } else if (Time.time-m_TimeStarted > 15f && distance > m_Params.casterRange || 
                        m_SelfStats.currentHealth < m_Params.lowHealth*m_SelfStats.maxHealth.GetValue()) {
                m_State = HangedManState.CasterEntrance;
            }
            m_SelfController.SetNavDestination(playerGameObject.transform.position);

        } else if (m_State == HangedManState.StealthedChase) {
            Debug.Log("StealthedChase");
            if (distance <= m_Params.inRange) {
                m_State = HangedManState.MeleeInRange;
                m_SpellManager.CastSpell(6);
            } else if (Time.time - m_StealthStart > m_Params.stealthDuration) {
                m_State = HangedManState.MeleeChase;
            } else if (Time.time-m_TimeStarted > 15f && distance > m_Params.casterRange || 
                        m_SelfStats.currentHealth < m_Params.lowHealth*m_SelfStats.maxHealth.GetValue()) {
                m_State = HangedManState.CasterEntrance;
            }
            m_SelfController.SetNavDestination(playerGameObject.transform.position + Vector3.Normalize(m_PlayerController.playerCamera.transform.right)*3f);

        } else if (m_State == HangedManState.MeleeInRange) {
            Debug.Log("MeleeInRange");
            if (distance < m_Params.inRange) {
                TryBasicAttack();
                // attack
            } else if (distance > m_Params.exitMeleeRange) {
                m_State = HangedManState.MeleeChase; // out of range
            }
            Vector3 currentTrajectory = selfGameObject.transform.position + selfGameObject.transform.forward;
            // Vector3 intendedTrajectory = m_PlayerController.playerCamera.transform.position;
            m_SelfController.SetNavDestination(currentTrajectory);

        } else if (m_State == HangedManState.CasterEntrance) {
            Debug.Log("CasterEntrance");
            m_State = HangedManState.CasterKite;
            m_SpellManager.CastSpell(1);
            SwitchToCasterMoveSpeed();

        } else if (m_State == HangedManState.CasterChase) {
            Debug.Log("CasterChase");
            if (distance < m_Params.casterRange) {
                m_State = HangedManState.CasterKite;
            } else if (distance < m_Params.returnMelee) {
                m_State = HangedManState.MeleeEntrance;
            } else if (m_SelfStats.currentHealth < m_Params.lowHealth*m_SelfStats.maxHealth.GetValue()) {
                m_State = HangedManState.CasterPanicEntrance;
            } else if (m_DamageBalanced < -50f && Random.value < .5) {
                m_DamageBalanced += 50f;
                m_State = HangedManState.CasterSummonEntrance;
            }
            m_SelfController.SetNavDestination(playerGameObject.transform.position);
            TrySpellAttack();

        } else if (m_State == HangedManState.CasterKite) {
            Debug.Log("CasterKite");
            if (distance > m_Params.tooFarRange) {
                m_State = HangedManState.CasterChase;
            } else if (distance < m_Params.returnMelee) {
                m_State = HangedManState.MeleeEntrance;
            } else if (m_SelfStats.currentHealth < m_Params.lowHealth*m_SelfStats.maxHealth.GetValue()) {
                m_State = HangedManState.CasterPanicEntrance;
            } else if (m_DamageBalanced < -50f && Random.value < .5) {
                m_DamageBalanced += 50f;
                m_State = HangedManState.CasterSummonEntrance;
            }
            Vector3 awayDirection = Vector3.Normalize(selfGameObject.transform.position - playerGameObject.transform.position);
            Vector3 rightDirection = Vector3.Cross(Vector3.up, awayDirection);
            Vector3 targetDirection = 0.8f*awayDirection + 0.2f*rightDirection;
            m_SelfController.SetNavDestination(selfGameObject.transform.position + targetDirection*10f);
            TrySpellAttack();

        } else if (m_State == HangedManState.CasterPanicEntrance) {
            Debug.Log("CasterPanicEntrance");
            m_State = HangedManState.CasterPanic;
            m_SpellManager.CastSpell(5);

        } else if (m_State == HangedManState.CasterPanic) {
            Debug.Log("CasterPanic");
            m_State = HangedManState.CasterPanic;
            Vector3 awayDirection = Vector3.Normalize(selfGameObject.transform.position - playerGameObject.transform.position);
            // Vector3 leftDirection = Vector3.Cross(awayDirection, Vector3.up);
            // Vector3 targetDirection = 0.5f*awayDirection + 0.5f*leftDirection;
            m_SelfController.SetNavDestination(selfGameObject.transform.position + awayDirection*10f);
            TryPanicedAttack();

        } else if (m_State == HangedManState.CasterSummonEntrance) {
            Debug.Log("CasterSummonEntrance");
            m_State = HangedManState.CasterKite;
            if (m_SpellManager.CastableShots(4) > 4) {
                m_State = HangedManState.CasterSummon;
                m_SelfController.StopMoving();
                m_SummoningStart = Time.time;
            } else {
                m_State = HangedManState.CasterKite;
            }

        } else if (m_State == HangedManState.CasterSummon) {
            Debug.Log("CasterSummon");
            if (Time.time - m_SummoningStart > m_Params.summoningTime) {
                m_State = HangedManState.CasterKite;
            }
            m_SpellManager.CastSpell(4);

        } else if (m_State == HangedManState.MeleeEntrance) {
            Debug.Log("MeleeEntrance");
            m_State = HangedManState.MeleeChase;
            SwitchToMeleeMoveSpeed();
        }

        m_PlayerPreviousPosition = playerGameObject.transform.position;
    }

    void TheHanging() {
        if (m_HangingState == HangingState.Begin) {
            Debug.Log("Begin");
            m_GFX.SetActive(true);
            m_SelfController.StopMoving();
            m_PlayerController.RemoveFocus();
            m_PlayerController.paused = true;
            if (Time.time - m_TimeStarted - m_Params.timesUp > m_Params.hangingWaitTime) {
                m_HangingState = HangingState.ModelChange;
            }

        } else if (m_HangingState == HangingState.ModelChange) {
            Debug.Log("ModelChange");
            m_HangingState = HangingState.PlayAnimation;
            m_TheHangingGraphic.SetActive(true);
            m_Animator.PlayHangingAnimation();
            m_MobileHealthBar.SetActive(false);
            // m_EnemyGraphic.SetActive(false);
            m_AnimationStartTime = Time.time;
            
        } else if (m_HangingState == HangingState.PlayAnimation) {
            Debug.Log("PlayAnimation");
            if (Time.time - m_AnimationStartTime > m_Params.theHangingAnimationDuration) {
                m_FallingStartTime = Time.time;
                m_PlayerController.gravityDownForce = 15f;
                m_HangingState = HangingState.RemoveWorld;
            }
            Transform playerTransform = playerGameObject.transform;
            Transform transform = selfGameObject.transform;
            Vector3 lookDir = Vector3.Normalize(transform.position - playerTransform.position);
            Quaternion lookRotation = Quaternion.LookRotation(lookDir);
            playerTransform.rotation = Quaternion.Slerp(playerTransform.rotation, lookRotation, Time.deltaTime * 5f);

            lookDir = Vector3.Normalize(playerTransform.position-transform.position);
            lookRotation = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 1f);

        } else if (m_HangingState == HangingState.RemoveWorld) {
            Debug.Log("RemoveWorld");
            if (Time.time-m_FallingStartTime < m_Params.fallingDuration) {
                m_Level.SetActive(false);
                m_PlayerStats.TakeDamage(0.2f);
                m_PlayerController.paused = false;
            } else {
                m_HangingState = HangingState.End;
            }
        } else {
            Debug.Log("End.");
        }
    }

    float GetDistance(GameObject a, GameObject b) {
        return Vector3.Distance(a.transform.position, b.transform.position);
    }

    void UpdateDamageTime(float dmg) {
        if (dmg < 0f) {
            m_LastTakenDamagedTime = Time.time;
        }
    }

    private bool TryBasicAttack()
    {
        if (m_LastTimeBasic + m_DelayBetweenAttacks*m_cdr < Time.time) {
            m_PlayerInteractable.Interact(selfGameObject);
            m_LastTimeBasic = Time.time;
            m_GFX.SetActive(true);
            return true;
        }
        return false;
    }

    private void SwitchToCasterMoveSpeed() {
        m_SelfStats.moveSpeed.AddModifier(0.5f);
        m_SelfController.UpdateSpeed();
    }

    private void SwitchToMeleeMoveSpeed() {
        m_SelfStats.moveSpeed.RemoveModifier(0.5f);
        m_SelfController.UpdateSpeed();
    }

    private void TrySpellAttack() {
        if (m_PlayerStats.moveSpeed.GetValue() < m_Params.slowEnough || Random.value < .01) {
            TryCastSmite();
        } else {
            if (CanSeePlayer()) {
                TryCastPierce();
            }
        }
    }

    private void TryPanicedAttack() {
        Aim();
        m_SpellManager.CastSpell(3);
        m_SpellManager.CastSpell(2);
    }

    private void TryCastSmite() {
        Aim();
        m_SpellManager.CastSpell(3);
    }

    private void TryCastPierce() {
        AimPierce();
        m_SpellManager.CastSpell(2);
    }

    private bool CanSeePlayer() {
        Vector3 screenPoint = m_SelfController.spellCamera.WorldToViewportPoint(playerGameObject.transform.position);
        bool onScreen = screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;
        return onScreen;
        // if (!onScreen) {
        //     Debug.Log("not on screen");
        //     return false;
        // }
        // Vector3 playerDirection = Vector3.Normalize(playerGameObject.transform.position - m_SelfController.spellCamera.transform.position);
        // if (Physics.Raycast(m_SelfController.spellCamera.transform.position, playerDirection, out RaycastHit hit))
        // {
        //     if (hit.transform == playerGameObject.transform) {
        //         return true;
        //     } else {
        //         Debug.Log("something inbetween player");
        //     }
        // } else {
        //     Debug.Log("can't hit player?");
        // }
        // return false;
    }

    private void AimPierce() {
        Vector3 playerVelocity = (playerGameObject.transform.position - m_PlayerPreviousPosition) / Time.deltaTime;
        float distance = GetDistance(selfGameObject, playerGameObject);
        Vector3 forwardDirection = Vector3.Normalize(playerGameObject.transform.position-selfGameObject.transform.position);
        Vector3 rightDirection = Vector3.Cross(Vector3.up, forwardDirection);
        float pierceSpeed = 30f;
        float Vr = Vector3.Dot(playerVelocity, rightDirection);
        float Vf = Mathf.Sqrt(pierceSpeed*pierceSpeed-Vr*Vr);
        // Vector3 velocityToShoot = Vr*rightDirection + Vf * forwardDirection;
        float travelTime = distance/Vf;
        Vector3 futurePosition = playerGameObject.transform.position + playerVelocity * travelTime; // rough
        futurePosition.y = Mathf.Clamp(futurePosition.y, 0.5f, 3f); // ignore crazy jumps until we calculate gravity

        selfGameObject.transform.LookAt(futurePosition);
    }

    private void Aim() {
        selfGameObject.transform.LookAt(playerGameObject.transform);
    }

}
