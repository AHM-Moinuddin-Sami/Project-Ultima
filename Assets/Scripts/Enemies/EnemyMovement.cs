using Pathfinding;
using UnityEngine;

/*
 * EnemyMovement
 * -------------
 * Handles physical enemy movement using A* Pathfinding Project Pro's
 * FollowerEntity.
 *
 * Movement (where the body goes) and facing (which way it looks) are
 * handled completely separately:
 *
 * - MoveTo()/Stop() drive FollowerEntity's position only.
 * - FaceTarget() manually rotates the transform every frame, regardless
 *   of what movement is doing. FollowerEntity's own updateRotation is
 *   disabled in Awake, because its built-in facing-direction parameter
 *   only takes effect once it *arrives* at a destination -- while it's
 *   actually travelling it faces its direction of travel instead. That
 *   makes it useless for "keep looking at the player while walking
 *   backwards away from them", which is the whole point of retreating
 *   and strafing, so we own rotation outright instead.
 *
 * Decoupling the two is also what makes the directional blend tree work
 * correctly: MoveX/MoveZ are the character's velocity in its own local
 * space, so once facing genuinely stops tracking movement direction,
 * walking backward while facing the player correctly reads as backward
 * local velocity (and strafing reads as sideways), with no per-direction
 * special-casing needed in this script.
 *
 * Animator tags required:
 * - Attack animations  -> "Attack"
 * - Stagger animations -> "Stagger"
 * - Walk/Run state     -> "Movement" (a directional blend tree using
 *   MoveX/MoveZ, with MoveSpeed as that state's Speed Parameter)
 */

[RequireComponent(typeof(FollowerEntity))]
public class EnemyMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Animation")]
    [SerializeField] private float walkAnimationReferenceSpeed = 1.544f;
    [SerializeField] private float maxAnimationSpeedMultiplier = 1.5f;

    [Header("Facing")]
    [SerializeField] private float facingDegreesPerSecond = 480f;

    private FollowerEntity follower;

    private bool waitingForMovementAnimation;

    private static readonly int IsMovingHash =
        Animator.StringToHash("IsMoving");

    private static readonly int MoveSpeedHash =
        Animator.StringToHash("MoveSpeed");

    private static readonly int MoveXHash =
        Animator.StringToHash("MoveX");

    private static readonly int MoveZHash =
        Animator.StringToHash("MoveZ");

    private void Awake()
    {
        follower = GetComponent<FollowerEntity>();

        // We are the only thing allowed to touch rotation -- see the
        // class comment for why FollowerEntity's own facing can't be
        // trusted to hold while it's actively moving.
        follower.updateRotation = false;
    }

    /*
     * Called every frame by EnemyAI to keep looking at the current
     * target (the player today; whichever enemy currently holds aggro
     * once multi-enemy targeting exists), independent of whatever
     * movement is doing. Safe to call from every state, including
     * while stopped, attacking, staggered, or mid-retreat/strafe.
     */
    public void FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            facingDegreesPerSecond * Time.deltaTime
        );
    }

    /*
     * Called by EnemyAI whenever the enemy should move somewhere.
     * Position only -- call FaceTarget() separately for facing.
     */
    public void MoveTo(Vector3 destination)
    {
        follower.SetDestination(destination);

        AnimatorStateInfo currentState =
            animator.GetCurrentAnimatorStateInfo(0);

        /*
         * An action animation currently owns the character.
         * Do not allow FollowerEntity to physically move.
         */
        if (currentState.IsTag("Attack") ||
            currentState.IsTag("Stagger"))
        {
            waitingForMovementAnimation = true;
            StopForAction();
            return;
        }

        /*
         * After an attack/stagger, request locomotion first.
         * Physical movement only resumes once the Movement animation
         * actually becomes active.
         */
        if (waitingForMovementAnimation)
        {
            animator.SetBool(IsMovingHash, true);
            animator.SetFloat(MoveSpeedHash, 1f);

            if (!currentState.IsTag("Movement"))
            {
                follower.simulateMovement = false;
                return;
            }

            waitingForMovementAnimation = false;
        }

        follower.simulateMovement = true;

        UpdateAnimator();
    }

    /*
     * Normal stop, such as standing in attack range.
     */
    public void Stop()
    {
        follower.simulateMovement = false;

        animator.SetBool(IsMovingHash, false);
        animator.SetFloat(MoveSpeedHash, 0f);
    }

    /*
     * Used when an Attack/Stagger interrupts locomotion.
     * Remembers that movement must wait for the Movement animation
     * before physically starting again.
     */
    public void StopForAction()
    {
        waitingForMovementAnimation = true;

        Stop();
    }

    private void UpdateAnimator()
    {
        float speed = follower.velocity.magnitude;

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

        // Direction relative to which way the character is FACING
        // (independently controlled by FaceTarget), not world space or
        // direction of travel. Below the moving threshold the velocity
        // direction is noisy, so just hold the last direction rather
        // than normalize noise.
        if (speed > 0.05f)
        {
            Vector3 localDirection =
                transform.InverseTransformDirection(follower.velocity) /
                speed;

            animator.SetFloat(MoveXHash, localDirection.x);
            animator.SetFloat(MoveZHash, localDirection.z);
        }
    }
}