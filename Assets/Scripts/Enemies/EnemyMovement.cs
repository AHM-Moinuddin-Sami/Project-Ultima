using Pathfinding;
using UnityEngine;

/*
 * EnemyMovement
 * -------------
 * Handles enemy pursuit using A* Pathfinding Project Pro's FollowerEntity.
 *
 * Responsibilities:
 * - detect the player within chase range
 * - approach the player, using FollowerEntity's own stopDistance to halt
 *   at a melee standoff instead of walking onto the player's exact
 *   position (which is what caused it to shove the player around)
 * - pass a facing direction into every destination update, so
 *   FollowerEntity's own movement controller handles turning to face
 *   the player smoothly, both while approaching and once stopped --
 *   no separate "who owns rotation" handoff needed like AIPath required
 * - stop while staggered
 * - drive the Walk animation's own playback rate from actual velocity
 *   so the stride matches how fast the enemy is really moving
 */

[RequireComponent(typeof(FollowerEntity))]
public class EnemyMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private EnemyCombat enemyCombat;
    [SerializeField] private Animator animator;

    [Header("Ranges")]
    [SerializeField] private float chaseRange = 12f;
    [SerializeField] private float attackRange = 2f;

    [Header("Animation")]
    // Longs_WalkFwd's own authored pace (AnimationClip.averageSpeed), so a
    // MoveSpeed of 1 plays the clip at the speed it was actually animated for.
    [SerializeField] private float walkAnimationReferenceSpeed = 1.544f;
    [SerializeField] private float maxAnimationSpeedMultiplier = 1.5f;

    private FollowerEntity follower;

    private static readonly int IsMovingHash =
        Animator.StringToHash("IsMoving");

    private static readonly int MoveSpeedHash =
        Animator.StringToHash("MoveSpeed");

    private void Awake()
    {
        follower = GetComponent<FollowerEntity>();
    }

    private void Update()
    {
        // Horizontal-only distance: the player's transform sits at
        // eye height while the enemy's sits near the ground, so a
        // full 3D distance would carry a large, constant vertical
        // offset that has nothing to do with melee range.
        Vector3 enemyToPlayer = player.position - transform.position;
        enemyToPlayer.y = 0f;
        float distanceToPlayer = enemyToPlayer.magnitude;

        // Stagger completely interrupts movement.
        if (enemyCombat.IsStaggered)
        {
            StopMoving();
            return;
        }

        // Player is outside detection range.
        if (distanceToPlayer > chaseRange)
        {
            StopMoving();
            return;
        }

        // Always head for the player's actual position and face them;
        // FollowerEntity's own stopDistance (set in the Inspector, kept
        // comfortably inside attackRange) halts the approach at melee
        // range on its own, and keeps enforcing the facing direction
        // even once stopped -- so this one call covers both chasing
        // and standing in melee, no separate state machine required.
        follower.SetDestination(player.position, enemyToPlayer.normalized);
        follower.canMove = true;

        if (distanceToPlayer <= attackRange)
        {
            enemyCombat.TryAttack();
        }

        UpdateAnimator();
    }

    private void StopMoving()
    {
        follower.canMove = false;

        animator.SetBool(IsMovingHash, false);
        animator.SetFloat(MoveSpeedHash, 0f);
    }

    private void UpdateAnimator()
    {
        float speed = follower.velocity.magnitude;

        // Hysteresis on the Walk/Idle switch -- velocity dithers right
        // around a single threshold while easing in/out of a stop,
        // which would otherwise flicker the animation.
        bool wasMoving = animator.GetBool(IsMovingHash);
        bool isMoving = wasMoving ? speed > 0.05f : speed > 0.2f;

        animator.SetBool(IsMovingHash, isMoving);

        float speedMultiplier = speed / walkAnimationReferenceSpeed;
        animator.SetFloat(
            MoveSpeedHash,
            Mathf.Clamp(speedMultiplier, 0f, maxAnimationSpeedMultiplier)
        );
    }
}
