using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
}
