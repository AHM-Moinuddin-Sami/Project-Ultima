using UnityEngine;
using UnityEngine.InputSystem;

/*
 * PlayerMovement
 * --------------
 * Handles first-person movement using Unity's CharacterController.
 *
 * Responsibilities:
 * - responsive WASD movement while grounded
 * - preserve horizontal momentum when jumping
 * - allow optional limited air control
 * - apply gravity
 * - handle jumping
 *
 * The horizontal velocity is stored separately from input so releasing
 * a movement key in midair does not instantly remove jump momentum.
 */

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Air Movement")]
    [SerializeField] private float airAcceleration = 2f;

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 1.5f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedGravity = -2f;

    private CharacterController characterController;

    private Vector2 moveInput;

    private Vector3 horizontalVelocity;
    private float verticalVelocity;

    private bool jumpRequested;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        bool isGrounded = characterController.isGrounded;

        Vector3 inputDirection =
            transform.right * moveInput.x +
            transform.forward * moveInput.y;

        inputDirection = Vector3.ClampMagnitude(inputDirection, 1f);

        if (isGrounded)
        {
            // Ground movement responds directly to player input.
            horizontalVelocity = inputDirection * moveSpeed;

            if (verticalVelocity < 0f)
            {
                verticalVelocity = groundedGravity;
            }

            if (jumpRequested)
            {
                verticalVelocity =
                    Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }
        else
        {
            // Preserve existing horizontal momentum in the air.
            // Input only adjusts it gradually instead of replacing it.
            Vector3 targetVelocity = inputDirection * moveSpeed;

            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity,
                targetVelocity,
                airAcceleration * Time.deltaTime
            );
        }

        jumpRequested = false;

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = horizontalVelocity;
        velocity.y = verticalVelocity;

        characterController.Move(velocity * Time.deltaTime);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            jumpRequested = true;
        }
    }
}