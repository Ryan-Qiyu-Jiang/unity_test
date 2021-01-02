using UnityEngine;

public class PlayerStatsController : CharacterStatsController
{
    PlayerCharacterController m_PlayerCharacterController;
    void Start() {
      m_PlayerCharacterController = GetComponent<PlayerCharacterController>();
      DebugUtility.HandleErrorIfNullGetComponent<PlayerCharacterController, PlayerStatsController>(m_PlayerCharacterController, this, gameObject);
    }
    public override void Die() {
        print("I AM DEED");
        m_PlayerCharacterController.paused = true;
    }

    void Update() {
      if (Input.GetKeyDown(KeyCode.Q))
      {
        TakeDamage(20);
      }
    }
}
