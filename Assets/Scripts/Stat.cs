using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct TempModifier {
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
    private List<float> modifiers = new List<float>();
    private List<TempModifier> temporaryModifiers = new List<TempModifier>();
    private float currentTime = 0;
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
        float endTime = currentTime + duration;
        temporaryModifiers.Add(new TempModifier(modifier, endTime));
    }

    void Update () {
        currentTime += Time.deltaTime;
        for (int i=0; i<temporaryModifiers.Count; i++) {
            float modifier = temporaryModifiers[i].modifier;
            float endTime = temporaryModifiers[i].endTime;
            if (currentTime >= endTime) {
                temporaryModifiers.RemoveAt(i);
                i -= 1;
            }
        }
    }
}
