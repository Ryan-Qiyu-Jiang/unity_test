using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct TempModifier {
    public float modifier;
    public float endTime;
    public string name;

    public TempModifier (float modifier, float endTime) {
        this.modifier = modifier;
        this.endTime = endTime;
        this.name = "";
    }
}

[System.Serializable]
public class Stat
{
    [SerializeField]
    private float baseValue = 0;
    public List<float> modifiers = new List<float>();
    public List<TempModifier> temporaryModifiers = new List<TempModifier>();
    // private float currentTime = 0;
    public float GetValue () {
        float val = baseValue;
        modifiers.ForEach(x => val *= x);
        temporaryModifiers.ForEach(x => val *= x.modifier);
        return val;
    }

    public void AddModifier (float modifier) {
        if (modifier != 1)
            modifiers.Add(modifier);
    }

    public void RemoveModifier (float modifier) {
        modifiers.Remove(modifier);
    }

    public void AddTemporaryModifier (float modifier, float duration) {
        float endTime = Time.time + duration;
        temporaryModifiers.Add(new TempModifier(modifier, endTime));
    }

    public void ManageTemporaryModifiers () {
        float time = Time.time;
        temporaryModifiers.RemoveAll(tmpMod => (time >= tmpMod.endTime));
    }
}
