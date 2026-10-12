using System;
using UnityEngine;

namespace FYP
{
    public class Health : MonoBehaviour
    {
        public float maxHealth = 100f;
        public float currentHealth;
        public bool invulnerable;

        // events for other scripts
        public Action<float> onDamaged;
        public Action onDied;

        // full health on start
        void Awake()
        {
            currentHealth = maxHealth;
        }

        // no health left
        public bool IsDead()
        {
            return currentHealth <= 0;
        }

        // apply damage, true if it landed
        public bool TakeDamage(float amount)
        {
            if (IsDead() || invulnerable)
                return false;

            currentHealth -= amount;
            if (currentHealth < 0)
                currentHealth = 0;

            if (onDamaged != null)
                onDamaged(amount);

            if (IsDead() && onDied != null)
                onDied();
            return true;
        }

        // back to full for a new attempt
        public void ResetHealth()
        {
            currentHealth = maxHealth;
            invulnerable = false;
        }
    }
}
