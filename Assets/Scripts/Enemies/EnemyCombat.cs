using System.Collections;
using UnityEngine;

/*
 * EnemyCombat
 * -----------
 * Handles enemy attack resolution and reactions.
 *
 * Responsibilities:
 * - play the attack animation
 * - damage the player when the animation's own impact event fires
 *   (see EnemyAnimationEvents, which forwards it from the model)
 * - detect blocked/parried attacks
 * - stagger after a successful parry
 * - prevent attacking while staggered
 */

public class EnemyCombat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Attack")]
    [SerializeField] private float damage = 20f;
    [SerializeField] private float attackCooldown = 2f;

    [Header("Parry")]
    [SerializeField] private float parryStaggerDuration = 1f;

    private static readonly int AttackHash =
        Animator.StringToHash("Attack");

    private static readonly int StaggerHash =
        Animator.StringToHash("Stagger");

    private bool isStaggered;
    private float nextAttackTime;

    public bool IsStaggered => isStaggered;

    public bool CanAttack =>
        !isStaggered && Time.time >= nextAttackTime;

    public void TryAttack()
    {
        if (!CanAttack)
        {
            return;
        }

        nextAttackTime = Time.time + attackCooldown;

        animator.SetTrigger(AttackHash);
    }

    // Called by EnemyAnimationEvents when the attack clip's impact
    // event fires, so the hit lands exactly when the swing connects.
    public void OnAttackImpact()
    {
        HitPlayer(playerHealth);
    }

    public void HitPlayer(PlayerHealth playerHealth)
    {
        if (isStaggered)
        {
            return;
        }

        HitResult result = playerHealth.TakeDamage(damage);

        if (result == HitResult.Parried)
        {
            StartCoroutine(ParryStagger());
        }
    }

    private IEnumerator ParryStagger()
    {
        isStaggered = true;

        animator.SetTrigger(StaggerHash);

        yield return new WaitForSeconds(parryStaggerDuration);

        isStaggered = false;
    }
}