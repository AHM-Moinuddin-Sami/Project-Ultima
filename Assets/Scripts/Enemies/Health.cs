using Pathfinding;
using UnityEngine;

/*
 * Health
 * ------
 * Stores health for any damageable object.
 *
 * Responsibilities:
 * - track current health
 * - receive damage
 * - play a death animation and disable AI/combat/collision, then
 *   destroy the object once the death animation has played out
 *
 * The animator reference is optional so this can still be used by
 * non-animated damageable props.
 */

public class Health : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Death")]
    [SerializeField] private Animator animator;
    [SerializeField] private float destroyDelay = 3f;

    private static readonly int DeathHash =
        Animator.StringToHash("Death");

    private static readonly int IsDeadHash =
        Animator.StringToHash("IsDead");

    private float currentHealth;
    private DamageFlash damageFlash;
    private bool isDead;

    public bool IsDead => isDead;

    private void Awake()
    {
        currentHealth = maxHealth;

        damageFlash = GetComponent<DamageFlash>();
    }

    public void TakeDamage(float damage)
    {
        if (isDead)
        {
            return;
        }

        currentHealth -= damage;

        damageFlash?.Flash();

        Debug.Log($"{name} took {damage} damage. Health: {currentHealth}");

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;

        if (animator != null)
        {
            animator.SetBool(IsDeadHash, true);
            animator.SetTrigger(DeathHash);
        }

        foreach (Collider hitCollider in GetComponentsInChildren<Collider>())
        {
            hitCollider.enabled = false;
        }

        FollowerEntity follower = GetComponent<FollowerEntity>();
        if (follower != null)
        {
            follower.canMove = false;
            follower.enabled = false;
        }

        EnemyMovement movement = GetComponent<EnemyMovement>();
        if (movement != null)
        {
            movement.enabled = false;
        }

        EnemyCombat combat = GetComponent<EnemyCombat>();
        if (combat != null)
        {
            combat.enabled = false;
        }

        Destroy(gameObject, destroyDelay);
    }
}