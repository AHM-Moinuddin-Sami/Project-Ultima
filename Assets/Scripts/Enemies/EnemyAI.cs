using UnityEngine;

/*
 * EnemyAI
 * -------
 * Makes the high-level combat decisions for the enemy.
 *
 * Current behaviour:
 *
 * Chase
 *   -> pursue the player
 *
 * Attack
 *   -> stop and run the 4-hit combo (Combo1..Combo4) for as long as
 *      the target stays in range; drops out of the chain into that
 *      step's own recovery animation the moment they're not, rather
 *      than always playing out the full combo
 *
 * Retreat
 *   -> move away from the player after attacking
 *   -> hand off to Strafe once enough distance has been created
 *
 * Strafe
 *   -> circle sideways around the player for a short beat
 *   -> hand off to Chase once the strafe has run its course
 *
 * Facing is handled every frame, independent of the state machine --
 * see the FaceTarget() call at the top of Update(). The enemy looks at
 * its target (the player, for now) continuously, whether it's chasing,
 * retreating, strafing, attacking, or staggered. That single call is
 * also the seam a future aggro system hooks into: whichever target
 * currently holds this enemy's aggro just gets passed there instead of
 * always being "player".
 *
 * EnemyAI does NOT directly control FollowerEntity or animations.
 * It tells EnemyMovement and EnemyCombat what it wants done.
 */

public class EnemyAI : MonoBehaviour
{
    private enum State
    {
        Chase,
        Attack,
        Retreat,
        Strafe
    }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private EnemyMovement movement;
    [SerializeField] private EnemyCombat combat;

    [Header("Ranges")]
    [SerializeField] private float chaseRange = 12f;
    [SerializeField] private float attackRange = 2f;

    [Header("Combo")]
    // Extra slack (beyond attackRange) allowed before the combo stops
    // chaining, so the player standing right at the edge of attackRange
    // doesn't flicker the combo on and off frame-to-frame.
    [SerializeField] private float comboContinueRangeBuffer = 0.4f;

    [Header("Retreat")]
    [SerializeField] private float retreatDistance = 3.5f;
    [SerializeField] private float retreatDestinationOffset = 5f;

    [Header("Strafe")]
    [SerializeField] private float strafeDuration = 1.8f;
    [SerializeField] private float strafeStepDistance = 3f;

    private State state = State.Chase;

    /*
     * Prevents us from leaving the Attack state during the tiny delay
     * between setting the Attack trigger and the Animator actually
     * entering the Attack state.
     */
    private bool attackAnimationStarted;

    /*
     * Which way we're circling this strafe, chosen when we enter the
     * state. +1 = one way around the player, -1 = the other.
     */
    private float strafeDirection = 1f;
    private float strafeTimer;

    private void Update()
    {
        Vector3 enemyToPlayer =
            player.position - transform.position;

        enemyToPlayer.y = 0f;

        float distanceToPlayer =
            enemyToPlayer.magnitude;

        // Look at the target no matter what else is happening. This is
        // deliberately unconditional -- every state below only decides
        // where the body goes, never which way it looks.
        movement.FaceTarget(player.position);

        /*
         * Stagger overrides every normal AI decision.
         */
        if (combat.IsStaggered)
        {
            movement.StopForAction();
            return;
        }

        switch (state)
        {
            case State.Chase:
                UpdateChase(enemyToPlayer, distanceToPlayer);
                break;

            case State.Attack:
                UpdateAttack(distanceToPlayer);
                break;

            case State.Retreat:
                UpdateRetreat(enemyToPlayer, distanceToPlayer);
                break;

            case State.Strafe:
                UpdateStrafe(enemyToPlayer, distanceToPlayer);
                break;
        }
    }

    private void UpdateChase(
        Vector3 enemyToPlayer,
        float distanceToPlayer)
    {
        if (distanceToPlayer > chaseRange)
        {
            movement.Stop();
            return;
        }

        /*
         * Once inside attack range, attempt an attack.
         */
        if (distanceToPlayer <= attackRange)
        {
            movement.Stop();

            if (combat.TryAttack())
            {
                state = State.Attack;
                attackAnimationStarted = false;

                movement.StopForAction();
            }

            return;
        }

        movement.MoveTo(player.position);
    }

    private void UpdateAttack(float distanceToPlayer)
    {
        movement.StopForAction();

        /*
         * Wait until the Animator has actually entered the attack.
         */
        if (combat.IsAttacking)
        {
            attackAnimationStarted = true;

            /*
             * Keep chaining the combo for as long as the target is
             * still within reach. The moment they're not, this simply
             * stops telling the Animator to continue -- whatever hit
             * is currently mid-swing still finishes (there's no
             * cosmetic way to cut it short), but it resolves into its
             * own recovery animation instead of chaining into the
             * next one. That's what lets the combo stop at any step.
             */
            combat.SetComboContinuation(
                distanceToPlayer <= attackRange + comboContinueRangeBuffer
            );

            return;
        }

        /*
         * Don't retreat during the frame between SetTrigger()
         * and actually entering the animation.
         */
        if (!attackAnimationStarted)
        {
            return;
        }

        /*
         * Attack animation has started and subsequently finished.
         */
        attackAnimationStarted = false;
        state = State.Retreat;
    }

    private void UpdateRetreat(
        Vector3 enemyToPlayer,
        float distanceToPlayer)
    {
        /*
         * Enough space has been created -- circle a bit before
         * charging back in, rather than beelining straight at them.
         */
        if (distanceToPlayer >= retreatDistance)
        {
            EnterStrafe();
            return;
        }

        if (enemyToPlayer.sqrMagnitude < 0.001f)
        {
            movement.Stop();
            return;
        }

        Vector3 awayFromPlayer =
            -enemyToPlayer.normalized;

        /*
         * Pick a point behind the enemy.
         *
         * FollowerEntity/A* handles finding a valid route rather than
         * us manually translating the enemy backwards. Facing is
         * handled separately every frame by Update(), so this call is
         * purely "walk to this point" -- the backward-walk animation
         * comes from that facing staying locked on the player while
         * the body moves away from them.
         */
        Vector3 retreatDestination =
            transform.position +
            awayFromPlayer * retreatDestinationOffset;

        movement.MoveTo(retreatDestination);
    }

    private void EnterStrafe()
    {
        state = State.Strafe;
        strafeTimer = 0f;
        strafeDirection = Random.value < 0.5f ? -1f : 1f;
    }

    private void UpdateStrafe(
        Vector3 enemyToPlayer,
        float distanceToPlayer)
    {
        if (distanceToPlayer > chaseRange)
        {
            movement.Stop();
            state = State.Chase;
            return;
        }

        strafeTimer += Time.deltaTime;

        if (strafeTimer >= strafeDuration)
        {
            state = State.Chase;
            return;
        }

        if (enemyToPlayer.sqrMagnitude < 0.001f)
        {
            movement.Stop();
            return;
        }

        /*
         * Sideways relative to the player, not the enemy's own
         * facing -- facing is locked onto the player independently,
         * so "sideways" has to be derived from the enemy-to-player
         * vector instead of transform.right.
         */
        Vector3 tangent = new Vector3(
            -enemyToPlayer.normalized.z,
            0f,
            enemyToPlayer.normalized.x
        ) * strafeDirection;

        /*
         * Continuously re-aimed at a point off to the side rather than
         * a fixed point in space, so this naturally traces an arc
         * around the player as position updates each frame.
         */
        Vector3 strafeDestination =
            transform.position + tangent * strafeStepDistance;

        movement.MoveTo(strafeDestination);
    }
}
