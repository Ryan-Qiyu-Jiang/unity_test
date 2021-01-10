using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StrategyParams : MonoBehaviour
{
    public float autoRange = 3f;
    public float minStealthRange = 3f;
    public float inRange = 5f;
    public float stealthDuration = 5f;
    public float lowHealth = .25f;
    public float timesUp = 60f*3; // the hanging wait duration
    public float exitMeleeRange = 3f;

    public float casterRange = 20f;
    public float tooFarRange = 30f;
    public float slowEnough = 1.5f;

    public float returnMelee = 7f;
    public float summoningTime = 3f;
    public float hangingWaitTime = 2f;
    public float theHangingAnimationDuration = 5f;
    public float fallingDuration = 15f;
}
