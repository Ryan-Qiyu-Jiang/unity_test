using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectLayers : MonoBehaviour
{
    #region Singleton
    public static ObjectLayers instance;

    void Awake ()
    {
        instance = this;
    }

    #endregion

    public int player = 8;
    public int enemy = 9;
    public int playerProjectile = 10;
    public int playerShield = 11;
    public int enemyProjectile = 12;
    public int enemyShield = 13;

    public void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (null == obj)
        {
            return;
        }
       
        obj.layer = newLayer;
       
        foreach (Transform child in obj.transform)
        {
            if (null == child)
            {
                continue;
            }
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

}
