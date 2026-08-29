using UnityEngine;

/*
 * FirstPersonAnimation
 * --------------------
 * Feeds basic gameplay state into the first-person Animator.
 *
 * Responsibilities:
 * - calculate current horizontal movement speed
 * - update movement animation parameters
 * - update grounded state
 *
 * Attack animation handling will be added later.
 */

public class FirstPersonAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterController characterController;

    private static readonly int MoveSpeedHash =
        Animator.StringToHash("MoveSpeed");

    private static readonly int IsGroundedHash =
        Animator.StringToHash("IsGrounded");

    private void Update()
    {
        Vector3 velocity = characterController.velocity;
        velocity.y = 0f;

        animator.SetFloat(MoveSpeedHash, velocity.magnitude);
        animator.SetBool(IsGroundedHash, characterController.isGrounded);
    }
}