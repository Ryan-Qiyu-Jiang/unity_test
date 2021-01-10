using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnemyManager : MonoBehaviour
{
    #region Singleton
    public static EnemyManager instance;

    void Awake ()
    {
        instance = this;
    }

    #endregion

    public List<GameObject> allEnemies;
    public GameObject enemy;
    public UnityAction<float> enemyDamageTaken;

    void Start() {
        CharacterStatsController stats = GetComponent<CharacterStatsController>();
        DebugUtility.HandleErrorIfNullGetComponent<CharacterStatsController, EnemyManager>(stats, this, gameObject);
        stats.onHealthChanged += HookDamage;
    }

    void HookDamage(float dmg) {
        enemyDamageTaken.Invoke(dmg);
    }

}
