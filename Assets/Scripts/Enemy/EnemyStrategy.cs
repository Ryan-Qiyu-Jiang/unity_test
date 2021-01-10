using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyStrategy
{
    public GameObject playerGameObject {get; set;}
    public GameObject selfGameObject {get; set;}

    public virtual void Init () {}
    public virtual void Move () {}
}
