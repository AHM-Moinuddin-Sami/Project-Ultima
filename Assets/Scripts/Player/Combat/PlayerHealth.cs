using UnityEngine;

/*
 * PlayerHealth
 * ------------
 * Handles damage received by the player.
 *
 * Responsibilities:
 * - apply full damage normally
 * - reduce damage while blocking
 * - negate damage during a parry window
 *
 * Later this can also trigger stagger, UI, death, stamina loss,
 * sounds, VFX, and parry reactions.
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

    public void TakeDamage(float damage)
    {
        if (combat.IsParrying)
        {
            Debug.Log("Parry!");
            return;
        }

        if (combat.IsBlocking)
        {
            damage *= blockDamageMultiplier;
        }

        currentHealth -= damage;

        Debug.Log($"Player took {damage} damage. Health: {currentHealth}");

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Player died.");
    }
}