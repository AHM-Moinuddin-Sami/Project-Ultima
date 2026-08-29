using UnityEngine;

/*
 * FirstPersonAnimation
 * --------------------
 * Feeds player locomotion state into the first-person hand Animator.
 *
 * Responsibilities:
 * - provide current horizontal movement speed
 * - provide grounded state
 * - provide sprinting state
 *
 * Melee attack parameters will be added later.
 */

public class FirstPersonAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovement playerMovement;

    private static readonly int MoveSpeedHash =
        Animator.StringToHash("MoveSpeed");

    private static readonly int IsGroundedHash =
        Animator.StringToHash("IsGrounded");

    private static readonly int IsSprintingHash =
        Animator.StringToHash("IsSprinting");

    private void Update()
    {
        animator.SetFloat(
            MoveSpeedHash,
            playerMovement.HorizontalSpeed
        );

        animator.SetBool(
            IsGroundedHash,
            playerMovement.IsGrounded
        );

        animator.SetBool(
            IsSprintingHash,
            playerMovement.IsSprinting
        );
    }
}