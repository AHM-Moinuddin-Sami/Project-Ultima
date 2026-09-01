using UnityEngine;

/*
 * PlayerHealth
 * ------------
 * Handles damage received by the player.
 *
 * Responsibilities:
 * - receive normal damage
 * - reduce blocked damage
 * - negate parried damage
 * - return the result so the attacker can react
 */

public class PlayerHealth : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FirstPersonCombat combat;

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Blocking")]
    [SerializeField, Range(0f, 1f)]
    private float blockDamageMultiplier = 0.25f;

    private float currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public HitResult TakeDamage(float damage)
    {
        if (combat.IsParrying)
        {
            Debug.Log("Parry!");

            return HitResult.Parried;
        }

        if (combat.IsBlocking)
        {
            damage *= blockDamageMultiplier;

            currentHealth -= damage;

            Debug.Log($"Blocked hit. Damage: {damage}. Health: {currentHealth}");

            if (currentHealth <= 0f)
            {
                Die();
            }

            return HitResult.Blocked;
        }

        currentHealth -= damage;

        Debug.Log($"Player took {damage} damage. Health: {currentHealth}");

        if (currentHealth <= 0f)
        {
            Die();
        }

        return HitResult.Hit;
    }

    private void Die()
    {
        Debug.Log("Player died.");
    }
}