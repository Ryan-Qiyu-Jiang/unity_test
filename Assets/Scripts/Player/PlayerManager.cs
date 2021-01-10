using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerManager : MonoBehaviour
{
    #region Singleton
    public static PlayerManager instance;

    void Awake ()
    {
        instance = this;
    }

    #endregion

    public List<GameObject> allPlayers;
    public GameObject player;

    public UnityAction<float> playerDamageTaken;

    void Start() {
        CharacterStatsController stats = GetComponent<CharacterStatsController>();
        DebugUtility.HandleErrorIfNullGetComponent<CharacterStatsController, PlayerManager>(stats, this, gameObject);
        stats.onHealthChanged += HookDamage;
    }

    void HookDamage(float dmg) {
        playerDamageTaken.Invoke(dmg);
    }
}
