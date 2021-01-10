using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum Status {
    LowPrecision
};

public class CharacterStatsController : MonoBehaviour
{
    public Stat maxHealth;
    public Stat cdr;
    public Stat moveSpeed;
    public Stat ap;
    
    public List<Status> afflictions = new List<Status>();

    public float currentHealth; //{ get; private set; }
    public UnityAction<float> onHealthChanged;

    void Awake() {
        currentHealth = maxHealth.GetValue();
    }

    public void TakeDamage (float damage) {
        currentHealth -= damage;
        if (onHealthChanged != null) {
            onHealthChanged.Invoke(damage);
        }

        if (currentHealth <= 0) {
            Die();
            return;
        }
        float maxHealthVal = maxHealth.GetValue();
        if (currentHealth > maxHealthVal) {
            currentHealth = maxHealthVal;
        }
    }

    void Update() {
        // print(Time.time);
        maxHealth.ManageTemporaryModifiers();
        cdr.ManageTemporaryModifiers();
        ap.ManageTemporaryModifiers();
        moveSpeed.ManageTemporaryModifiers();
    }

    public bool HasStatus(Status status) {
        return afflictions.Contains(status);
    }

    public void AddStatus(Status status) {
        if (!HasStatus(status)) {
            print("Adding status");
            afflictions.Add(status);
        }
    }

    public void RemoveStatus(Status status) {
        if (HasStatus(status)) {
            afflictions.Remove(status);
        }
    }

    public virtual void Die() {}

}
