using System;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    public event EventHandler OnHealthChanged;
    public event EventHandler OnDead;

    [SerializeField] int maxHealth = 100;
    [SerializeField] int currentHealth;

    void Awake()
    {
        if (currentHealth == 0)
            currentHealth = maxHealth;
    }

    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;

        if (currentHealth < 0)
            currentHealth = 0;

        OnHealthChanged?.Invoke(this, EventArgs.Empty);

        if (currentHealth == 0)
            Die();
    }

    void Die()
    {
        OnDead?.Invoke(this, EventArgs.Empty);
    }

    public float CurrentHealthNormalized() => (float)currentHealth / maxHealth;

    public int MaxHealth() => maxHealth;

    public int CurrentHealth() => currentHealth;
}
