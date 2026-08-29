using UnityEngine;

/*
 * Health
 * ------
 * Stores health for any damageable object.
 *
 * Responsibilities:
 * - track current health
 * - receive damage
 * - destroy the object when health reaches zero
 *
 * This is intentionally generic so enemies, props, and other
 * damageable objects can all use the same component.
 */

public class Health : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    private float currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        Debug.Log($"{name} took {damage} damage. Health: {currentHealth}");

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        Destroy(gameObject);
    }
}