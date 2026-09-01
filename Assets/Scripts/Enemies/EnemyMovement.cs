using Pathfinding;
using UnityEngine;

/*
 * EnemyMovement
 * -------------
 * Handles enemy pursuit using A* Pathfinding Project Pro's FollowerEntity.
 *
 * Responsibilities:
 * - detect the player within chase range
 * - approach the player using FollowerEntity
 * - stop at melee distance
 * - face the player while approaching and while stopped
 * - stop movement during attacks and staggers
 * - wait for the movement animation to actually begin before physically
 *   moving again after an attack or stagger
 * - drive walk animation playback speed from actual movement velocity
 *
 * Animator state tags required:
 * - Attack animations  -> "Attack"
 * - Stagger animations -> "Stagger"
 * - Walk/Run state     -> "Movement"
 *
 * Important:
 * Idle should NOT use the "Movement" tag. We specifically wait until
 * the walk/run animation has actually become active before allowing
 * FollowerEntity to move after an action.
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
    [SerializeField] private float walkAnimationReferenceSpeed = 1.544f;
    [SerializeField] private float maxAnimationSpeedMultiplier = 1.5f;

    private FollowerEntity follower;

    // Becomes true whenever an attack/stagger interrupts movement.
    // We then wait until the Animator has actually entered its
    // Movement state before enabling physical movement again.
    private bool waitingForMovementAnimation;

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
        Vector3 enemyToPlayer = player.position - transform.position;
        enemyToPlayer.y = 0f;

        float distanceToPlayer = enemyToPlayer.magnitude;

        AnimatorStateInfo currentState =
            animator.GetCurrentAnimatorStateInfo(0);

        bool isInAttack = currentState.IsTag("Attack");
        bool isInStagger = currentState.IsTag("Stagger");

        /*
         * An attack/stagger owns the character completely.
         *
         * We check the Animator itself instead of relying only on
         * EnemyCombat timers because the actual animation may still
         * be playing or transitioning.
         */
        if (enemyCombat.IsStaggered || isInAttack || isInStagger)
        {
            waitingForMovementAnimation = true;

            StopMoving();
            return;
        }

        // Player is outside detection range.
        if (distanceToPlayer > chaseRange)
        {
            waitingForMovementAnimation = false;

            StopMoving();
            return;
        }

        /*
         * Keep the path destination updated even while movement is
         * temporarily locked.
         */
        follower.SetDestination(
            player.position,
            enemyToPlayer.normalized
        );

        /*
         * If an attack/stagger just ended, tell the Animator that
         * we want to start locomotion.
         *
         * Physical movement remains disabled until the Animator has
         * actually reached the Movement state.
         */
        if (waitingForMovementAnimation)
        {
            animator.SetBool(IsMovingHash, true);

            if (!currentState.IsTag("Movement"))
            {
                follower.simulateMovement = false;
                animator.SetFloat(MoveSpeedHash, 1f);
                return;
            }

            // Walk animation has actually begun.
            waitingForMovementAnimation = false;
        }

        follower.simulateMovement = true;

        if (distanceToPlayer <= attackRange)
        {
            enemyCombat.TryAttack();
        }

        UpdateAnimator();
    }

    private void StopMoving()
    {
        follower.simulateMovement = false;

        animator.SetBool(IsMovingHash, false);
        animator.SetFloat(MoveSpeedHash, 0f);
    }

    private void UpdateAnimator()
    {
        float speed = follower.velocity.magnitude;

        // Hysteresis prevents Idle/Walk flickering around zero velocity.
        bool wasMoving = animator.GetBool(IsMovingHash);
        bool isMoving = wasMoving
            ? speed > 0.05f
            : speed > 0.2f;

        animator.SetBool(IsMovingHash, isMoving);

        float speedMultiplier =
            speed / walkAnimationReferenceSpeed;

        animator.SetFloat(
            MoveSpeedHash,
            Mathf.Clamp(
                speedMultiplier,
                0f,
                maxAnimationSpeedMultiplier
            )
        );
    }
}