using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LevelsManager : MonoBehaviour
{
    #region Singleton
    public static LevelsManager instance;

    void Awake ()
    {
        instance = this;
    }

    #endregion

    public GameObject level;

}
