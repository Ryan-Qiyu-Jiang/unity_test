using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
public class CharacterStatsController : MonoBehaviour
{
    public Stat maxHealth;
    public Stat cdr;
    public Stat moveSpeed;
    public Stat ap;

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

    public virtual void Die() {}

}
